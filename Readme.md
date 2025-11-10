# 🏨 Hotel Room Booking API

A RESTful hotel room booking API built with ASP.NET and Aspire.
Uses EFCore with PostgreSQL as the persistence layer.

A version is deployed to Azure:
- View OpenAPI at https://hotelroombooking.gentlepond-023a01ce.uksouth.azurecontainerapps.io/scalar/
- Hit endpoints like https://hotelroombooking.gentlepond-023a01ce.uksouth.azurecontainerapps.io/hotels
```bash
curl 'https://hotelroombooking.gentlepond-023a01ce.uksouth.azurecontainerapps.io/hotels'
```

## 🚀 Local Development Quick Start

### Prerequisites

- .NET 9.0 SDK
- .NET Aspire
- Container runtime - Docker (to run PostgreSQL)


### Running the Application locally

The app leans on [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) to enable local testing and development.

1. **Run the AppHost project in development mode**
   ```bash
   cd HotelRoomBooking.AppHost
   dotnet run --launch-profile http
   ```

2. **Access the App**
- Aspire Dashboard: http://localhost:15057
- API: http://localhost:5160
- Scalar UI: http://localhost:5160/scalar/
- OpenAPI spec: http://localhost:5160/openapi/v1.json
- View PostgreSQL data in Pgweb: http://localhost:50164

3. **Send requests to the API**
- Use the provided [HotelRoomBooking.http](HotelRoomBooking/HotelRoomBooking.http) file for manual testing in Visual Studio or Rider
- Use the Scalar UI at [/scalar](http://localhost:5160/scalar/) for interactive API documentation and testing

## 🎯 Overview

### Business Rules

**Hotels have 3 room types: single, double, deluxe**

- This is represented by an enum `RoomType`
- Seperate Room object types were considered, but the extra complexity did not appear necessary.
  - RoomTypes do not encode any additional rules.
  - For example, no constraint is given about the number of beds in a room.

**Hotels have 6 rooms**

- This is enforced by a `Length` request model validation on the `POST /hotels` endpoint.
- A [PostgreSQL trigger](https://www.postgresql.org/docs/current/sql-createtrigger.html) was considered
  but would be less explicit and less flexible to changes in this buisness rule.


**A room cannot be double booked for any given night**

- This is enforced by a database-level constraint on the `BookedNight` table.
  - The composite key `(RoomId, Date)` prevents double-bookings.


**Any booking at the hotel must not require guests to change rooms at any point during their stay**

- This is encoded in a Many-To-One relationship between `Booking` and `Room`.

**Booking numbers should be unique. There should not be overlapping at any given time**

- This is enforced in a simple way, by making the `BookingReference` a primary key.
  - UseIdentityAlwaysColumn() adds a layer of protection, preventing the app setting an ID
  - Using GUID may be a better approach, staying unique in the face of new databases

**A room cannot be occupied by more people than its capacity**

- This is enforced by an application layer check, comparing `Room` capacity to the `BookingRequest`


## ✨ Challenge Requirements

### Business Functionality

- **Find a hotel based on its name.**
    - a POST endpoint is provided. For example
```bash
curl 'http://localhost:5160/hotels/{hotelName}'
```

2. **Find available rooms between two dates for a given number of people.**
    - a GET endpoint is provided on the Hotel resource
```bash
curl 'http://localhost:5160/hotels/{hotelName}/available-rooms?checkin=2025-11-07&checkout=2025-11-09&numberOfGuests=2'
```

3. **Book a room.**
    - a POST endpoint is provided on the Hotel resource
```bash
curl 'http://localhost:5160/hotels/{hotelName}/bookings' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
  "roomType": "Single",
  "guestId": "Bob",
  "numberOfGuests": 1,
  "checkInDate": "2025-11-07",
  "checkOutDate": "2025-11-09"
}'
```

4. **Find booking details based on a booking reference.**
    - a GET endpoint is provided on the Bookings resource
```bash
curl http://localhost:5160/bookings/1
```

### Technical Requirements

**The API must be testable**
- OpenAPI documentation is at [Documentation/hotelroombooking-openapi.yaml](Documentation/hotelroombooking-openapi.yaml)
  - Can be also accesed http://localhost:5160/openapi/v1.json
  - Or https://hotelroombooking.gentlepond-023a01ce.uksouth.azurecontainerapps.io/scalar/

**For testing purposes the API should expose functionality to allow for
  seeding and resetting the data** 
  - A `/tesdata` resource is provided (note that this not a RESTful endpoint)
  - Data can be seeded with a POST request to `/testdata`

```bash
```bash
curl http://localhost:5160/testdata \
--request POST
```
- data can be reset with a DELETE request to `/testdata`
```bash
curl http://localhost:5160/testdata \
  --request DELETE
```



## 🧪 Testing

### Integration Tests

The project includes example integration tests using .NET Aspire testing infrastructure.
Unit test have yet to be added.

```bash
cd HotelRoomBooking.IntegrationTest
dotnet test
```

**Test Coverage:**
- Empty hotel list retrieval
- Hotel creation
- Booking creation
- Conflict detection when all rooms are booked

### Manual Testing

Use the provided `HotelRoomBooking.http` file for manual testing in Visual Studio or Rider, or use the Scalar UI at `/scalar/`.

## 🚢 Deployment

### Azure Deployment

Prerequisits - Azure Developer CLI (azd)

The project is configured for Azure deployment via .NET Aspire.
To deploy run:
```bash
azd up
```

Further instructions on this page https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment


### Production Considerations

1. **Database Migrations**: Manually create database and apply migrations instead of `EnsureMigratedAsync()`
2. **Logging**: Configure application insights or logging providers
3. **HTTPS**: Ensure HTTPS is enabled in production
4. **Rate Limiting**: Consider implementing rate limiting for production

### Design Decisions

1. **Minimal APIs**: Used ASP.NET Core Minimal APIs for a clean, modern approach
3. **Domain Models**: Rich domain models with validation logic
5. **Database-first Constraints**: Unique constraints and composite keys at database level
6. **Resilience Patterns**: Polly retry policies for race condition handling
7. **Validation**: Fluent validation using data annotations and custom validators

## 📝 Additional Notes

### Room Matching Logic

The booking system uses the following priority for room selection:

1. If `roomName` is specified, match by name (and validate capacity/type if provided)
2. If `roomType` is specified, match by type and capacity
3. Otherwise, match by capacity only
4. Select the first available room matching the criteria

### Date Handling

- All dates are stored as `DateOnly` (date without time)
- Check-out date is exclusive (guests check out on this date, not stay)
- Bookings are stored as individual `BookedNight` records for each night

### Concurrency Handling

The system handles concurrent booking requests using optimistic concurrency
strategies:

1. Database-level unique constraints (prevents duplicates)
2. Resilience strategies with retry logic (handles race conditions)

Pessimistic concurrency was considered, but given writes are expected to be 
rare when compared to reads, optimistic concurrency should provide better query performance.

### Further Work

1. Add a DB managed Hotel.Id primary key
   1. Add unique constraint to the name (possibly with case sensitivity, trim whitespace ect)
1. Increase test coverage including unit tests
2. Add logging (some is already built in)
   1. Consider structured logging
3. Impove error response messages
   1. Utilise problem details consistently
4. Clarify requirements such as
   1. Should room names be unique within a Hotel
   2. Should room types have a defined size
5. Consider authentication/authorization
6. Handle transient PostgreSQL / network errors with appropriate level of retries
7. Consider API versioning

## 📄 License

This project is a technical challenge implementation and is provided as-is.

### Acknowledgments

- Uses .NET Aspire for cloud-native development
- PostgreSQL for data storage
- Scalar for API documentation
