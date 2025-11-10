var options = new DistributedApplicationOptions { Args = args };

var builder = DistributedApplication.CreateBuilder(options);

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var postgresdb = postgres.AddDatabase("hotelbooking");

builder.AddProject<Projects.HotelRoomBooking>("hotelroombooking")
    .WithReference(postgresdb)
    .WaitForStart(postgres)
    .WithExternalHttpEndpoints();

builder.Build().Run();
