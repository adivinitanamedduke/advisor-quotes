using System.Net;
using System.Net.Http.Json;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Responses;
using Microsoft.EntityFrameworkCore;
using Tests.Integration.Infrastructure;

namespace Tests.Integration.API.Quotes
{
    [Collection(PostgresCollection.Name)]
    public class QuoteApiTests(QuotesApiFixture fixture) : IAsyncLifetime
    {
        private const string BaseUrl = "/api/v1/Quote";
        private const string AdviserHeader = "X-Adviser-Id";
        private const string KeyHeader = "Idempotency-Key";
        private const string AdviserA = "adviser-a";
        private const string AdviserB = "adviser-b";

        public Task InitializeAsync() =>
            PostgresAvailability.SkipReason is null ? fixture.ResetDatabaseAsync() : Task.CompletedTask;

        public Task DisposeAsync() => Task.CompletedTask;

        private HttpClient Client(string? adviserId = AdviserA)
        {
            var client = fixture.Factory.CreateClient();
            if (adviserId is not null)
            {
                client.DefaultRequestHeaders.Add(AdviserHeader, adviserId);
            }

            return client;
        }

        private static CreateQuoteRequest CreateRequest(decimal cover = 1_000_000m, string notes = "Integration test") => new()
        {
            Client = new QuoteClientRequest { FullName = "Thandi Mokoena", DateOfBirth = new DateOnly(1985, 4, 12) },
            Product = new QuoteProductRequest { Code = "TERM_LIFE", CoverAmount = cover, TermYears = 20 },
            Notes = notes
        };

        private static Task<HttpResponseMessage> PostCreateAsync(HttpClient client, CreateQuoteRequest request, string? key = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl) { Content = JsonContent.Create(request) };
            if (key is not null)
            {
                message.Headers.Add(KeyHeader, key);
            }

            return client.SendAsync(message);
        }

        private static async Task<QuoteDto> ReadQuoteAsync(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<QuoteDto>())!;

        private async Task<int> CountQuotesAsync()
        {
            await using var db = fixture.CreateDbContext();
            return await db.Quotes.CountAsync();
        }

        private static Task<HttpResponseMessage> PutStatusAsync(HttpClient client, Guid identifier, string status, string? reason = null) =>
            client.PutAsJsonAsync($"{BaseUrl}/{identifier}/status", new UpdateQuoteStatusRequest { Status = status, Reason = reason });

        [PostgresFact]
        public async Task Create_WithValidRequest_Returns201WithLocationAndDraftQuote()
        {
            // Act
            var response = await PostCreateAsync(Client(), CreateRequest());

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var quote = await ReadQuoteAsync(response);
            Assert.Equal("Draft", quote.Status);
            Assert.Matches("^QT-\\d{6}$", quote.Reference);
            Assert.Equal(850m, quote.MonthlyPremium);
            Assert.Equal("ZAR", quote.Currency);
            Assert.Single(quote.StatusHistory);
            Assert.Equal($"{BaseUrl}/{quote.Identifier}", response.Headers.Location!.AbsolutePath, ignoreCase: true);
            Assert.False(response.Headers.Contains("Idempotent-Replayed"));
        }

        [PostgresFact]
        public async Task Create_ReplayedWithSameKey_Returns200WithSameIdentifier()
        {
            // Arrange
            var client = Client();
            var first = await ReadQuoteAsync(await PostCreateAsync(client, CreateRequest(), "key-replay"));

            // Act
            var replay = await PostCreateAsync(client, CreateRequest(), "key-replay");

            // Assert
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal("true", replay.Headers.GetValues("Idempotent-Replayed").Single());
            Assert.Equal(first.Identifier, (await ReadQuoteAsync(replay)).Identifier);
            Assert.Equal(1, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Create_IdenticalBodyWithoutKey_ReplaysInsideWindow()
        {
            // Arrange
            var client = Client();
            var first = await ReadQuoteAsync(await PostCreateAsync(client, CreateRequest()));

            // Act
            var second = await PostCreateAsync(client, CreateRequest());

            // Assert
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(first.Identifier, (await ReadQuoteAsync(second)).Identifier);
            Assert.Equal(1, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Create_KeyReusedWithDifferentBody_Returns409()
        {
            // Arrange
            var client = Client();
            await PostCreateAsync(client, CreateRequest(), "key-conflict");

            // Act
            var response = await PostCreateAsync(client, CreateRequest(cover: 2_000_000m), "key-conflict");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(409, error!.StatusCode);
            Assert.Equal(1, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Create_ParallelDoubleSubmit_CreatesOneQuote()
        {
            // Arrange
            var client = Client();

            // Act
            var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PostCreateAsync(client, CreateRequest())));

            // Assert
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            var identifiers = await Task.WhenAll(responses.Select(async r => (await ReadQuoteAsync(r)).Identifier));
            Assert.Single(identifiers.Distinct());
            Assert.Equal(1, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Create_ParallelSameKeyDifferentBodies_CreatesOneQuoteAndRejectsTheRest()
        {
            // Arrange
            var client = Client();

            // Act: different bodies take different fingerprint locks, so only the unique key index can settle this
            var responses = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(i => PostCreateAsync(client, CreateRequest(cover: 1_000_000m + i), "key-race")));

            // Assert
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.Equal(1, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Create_WhenPricingRuleBreached_Returns400()
        {
            // Arrange
            var request = CreateRequest();
            request.Product.Code = FakePremiumCalculator.UnknownProduct;

            // Act
            var response = await PostCreateAsync(Client(), request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task StatusChange_FullPath_RecordsThreeHistoryRows()
        {
            // Arrange
            var client = Client();
            var created = await ReadQuoteAsync(await PostCreateAsync(client, CreateRequest()));

            // Act
            var sent = await PutStatusAsync(client, created.Identifier, "SentToClient", "Emailed");
            var accepted = await PutStatusAsync(client, created.Identifier, "Accepted", "Client signed");
            var fetched = await client.GetAsync($"{BaseUrl}/{created.Identifier}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

            var quote = await ReadQuoteAsync(fetched);
            Assert.Equal("Accepted", quote.Status);
            Assert.True(quote.UpdatedAt > quote.CreatedAt);
            Assert.Collection(quote.StatusHistory,
                h => { Assert.Null(h.FromStatus); Assert.Equal("Draft", h.ToStatus); },
                h => { Assert.Equal("Draft", h.FromStatus); Assert.Equal("SentToClient", h.ToStatus); Assert.Equal("Emailed", h.Reason); },
                h => { Assert.Equal("SentToClient", h.FromStatus); Assert.Equal("Accepted", h.ToStatus); Assert.Equal(AdviserA, h.ChangedBy); });
        }

        [PostgresFact]
        public async Task StatusChange_BadTransition_Returns400AndLeavesQuoteUnchanged()
        {
            // Arrange
            var client = Client();
            var created = await ReadQuoteAsync(await PostCreateAsync(client, CreateRequest()));

            // Act
            var response = await PutStatusAsync(client, created.Identifier, "Accepted");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var quote = await ReadQuoteAsync(await client.GetAsync($"{BaseUrl}/{created.Identifier}"));
            Assert.Equal("Draft", quote.Status);
            Assert.Single(quote.StatusHistory);
        }

        [PostgresFact]
        public async Task StatusChange_ForAnotherAdvisersQuote_Returns404()
        {
            // Arrange
            var created = await ReadQuoteAsync(await PostCreateAsync(Client(AdviserA), CreateRequest()));

            // Act
            var response = await PutStatusAsync(Client(AdviserB), created.Identifier, "SentToClient");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [PostgresFact]
        public async Task Get_AnotherAdvisersQuote_Returns404()
        {
            // Arrange
            var created = await ReadQuoteAsync(await PostCreateAsync(Client(AdviserA), CreateRequest()));

            // Act
            var response = await Client(AdviserB).GetAsync($"{BaseUrl}/{created.Identifier}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(404, error!.StatusCode);
        }

        [PostgresFact]
        public async Task Create_WithoutAdviserHeader_Returns400()
        {
            // Act
            var response = await PostCreateAsync(Client(adviserId: null), CreateRequest());

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Contains(AdviserHeader, error!.Message);
            Assert.Equal(0, await CountQuotesAsync());
        }

        [PostgresFact]
        public async Task Search_ReturnsOnlyTheAdvisersQuotesFilteredByStatus()
        {
            // Arrange
            var clientA = Client(AdviserA);
            var draft = await ReadQuoteAsync(await PostCreateAsync(clientA, CreateRequest(notes: "one")));
            var declined = await ReadQuoteAsync(await PostCreateAsync(clientA, CreateRequest(notes: "two")));
            await PutStatusAsync(clientA, declined.Identifier, "Declined");
            await PostCreateAsync(Client(AdviserB), CreateRequest(notes: "other adviser"));

            // Act
            var all = await clientA.PostAsJsonAsync($"{BaseUrl}/search", new SearchQuotesRequest());
            var drafts = await clientA.PostAsJsonAsync($"{BaseUrl}/search", new SearchQuotesRequest { Status = "Draft" });

            // Assert
            var allPage = (await all.Content.ReadFromJsonAsync<PagedResult<QuoteSummaryDto>>())!;
            Assert.Equal(2, allPage.TotalCount);
            Assert.Equal(declined.Identifier, allPage.Items[0].Identifier);

            var draftPage = (await drafts.Content.ReadFromJsonAsync<PagedResult<QuoteSummaryDto>>())!;
            Assert.Equal(draft.Identifier, Assert.Single(draftPage.Items).Identifier);
        }
    }
}
