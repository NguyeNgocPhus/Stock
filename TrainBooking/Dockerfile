FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TrainBooking.sln .
COPY TrainBooking.Api/TrainBooking.Api.csproj TrainBooking.Api/
RUN dotnet restore TrainBooking.Api/TrainBooking.Api.csproj

COPY TrainBooking.Api/ TrainBooking.Api/
RUN dotnet publish TrainBooking.Api/TrainBooking.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TrainBooking.Api.dll"]
