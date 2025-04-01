# Build stage: use the .NET SDK to restore and publish the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY BankApp/*.csproj ./
COPY BankApp/. .
RUN dotnet restore

RUN dotnet build *.csproj -c Release -o /app/build

FROM build AS publish
# Copy the rest of the application code and publish it in Release configuration
RUN dotnet publish -c Release -o /app/publish

# Runtime stage: use Nginx to serve the static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
