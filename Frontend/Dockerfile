# Build stage: use the .NET SDK to restore and publish the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY BankApp/*.csproj ./
RUN dotnet restore

# Copy the rest of the application code and publish it in Release configuration
COPY BankApp/. ./
RUN dotnet publish -c Release -o /app/out

# Runtime stage: use the ASP.NET Core runtime to serve the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Expose the port your app runs on (adjust if necessary)
EXPOSE 5002

# Start the application. Adjust "Banka.Frontend.dll" to match your project's output DLL.
ENTRYPOINT ["dotnet", "Banka.Frontend.dll"]
