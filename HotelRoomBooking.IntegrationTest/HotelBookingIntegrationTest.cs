using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelRoomBooking.IntegrationTest;

public class HotelBookingIntegrationTest
{
    private const int DefaultTimeoutMilliseconds = 10000;

    [Test, CancelAfter(DefaultTimeoutMilliseconds)]
    public async Task Should_ReturnEmpty_When_NoHotels(CancellationToken cancellationToken)
    {
        // [arrange]
        var httpClient = await CreateApplicationHttpClientAsync(cancellationToken);

        // [act]
        using var response = await httpClient.GetAsync("/hotels", cancellationToken);

        // [assert]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test, CancelAfter(DefaultTimeoutMilliseconds)]
    public async Task Should_ReturnCreated_When_ValidHotelPosted(CancellationToken cancellationToken)
    {
        // [arrange]
        var httpClient = await CreateApplicationHttpClientAsync(cancellationToken);

        var hotelJson = await LoadTestDataAsync("Data/Hotel.json");

        // [act]
        using var response = await httpClient.PostAsync("/hotels", CreateJsonContent(hotelJson), cancellationToken);

        // [assert]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test, CancelAfter(DefaultTimeoutMilliseconds)]
    public async Task Should_ReturnCreated_When_BookingPosted(CancellationToken cancellationToken)
    {
        // [arrange]
        var httpClient = await CreateApplicationHttpClientAsync(cancellationToken);
        await PopulateHotel(httpClient, cancellationToken);

        var bookingJson = await LoadTestDataAsync("Data/BookingRequest.json");

        // [act]
        using var response = await httpClient.PostAsync("/hotels/Central/bookings", CreateJsonContent(bookingJson), cancellationToken);

        // [assert]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test, CancelAfter(DefaultTimeoutMilliseconds)]
    public async Task Should_ReturnConflict_When_AllRoomsBooked(CancellationToken cancellationToken)
    {
        // [arrange]
        var httpClient = await CreateApplicationHttpClientAsync(cancellationToken);
        await PopulateHotel(httpClient, cancellationToken);

        var bookingJson = await LoadTestDataAsync("Data/BookingRequest.json");

        // Book all 2 person rooms
        foreach (var _ in Enumerable.Repeat(1, 4))
        {
            await httpClient.PostAsync("/hotels/Central/bookings", CreateJsonContent(bookingJson), cancellationToken);
        }

        // [act]
        using var response = await httpClient.PostAsync("/hotels/Central/bookings", CreateJsonContent(bookingJson), cancellationToken);

        // [assert]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    private static async Task<HttpClient> CreateApplicationHttpClientAsync(CancellationToken cancellationToken)
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.HotelRoomBooking_AppHost>(cancellationToken);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        var app = await appHost.BuildAsync(cancellationToken).WaitAsync(cancellationToken);

        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("hotelroombooking", cancellationToken)
            .WaitAsync(cancellationToken);

        return app.CreateHttpClient("hotelroombooking");;
    }

    private static async Task PopulateHotel(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var hotelJson = await LoadTestDataAsync("Data/Hotel.json");
        await httpClient.PostAsync("/hotels", CreateJsonContent(hotelJson), cancellationToken);
    }

    private static StringContent CreateJsonContent(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private static async Task<string> LoadTestDataAsync(string fileName)
    {
        var assemblyPath = Path.GetDirectoryName(typeof(HotelBookingIntegrationTest).Assembly.Location);
        var filePath = Path.Combine(assemblyPath, fileName);
        return await File.ReadAllTextAsync(filePath);
    }
}