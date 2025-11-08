var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var postgresdb = postgres.AddDatabase("hotelbooking");

builder.AddProject<Projects.HotelRoomBooking>("hotelroombooking")
    .WithReference(postgresdb)
    .WaitForStart(postgres);

builder.Build().Run();
