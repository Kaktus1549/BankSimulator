# Build stage: use the .NET SDK to restore and publish the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY BankApp/*.csproj ./
RUN dotnet restore

# Copy the rest of the application code and publish it in Release configuration
COPY BankApp/. ./
RUN dotnet publish -c Release -o /app/out

# Runtime stage: use Nginx to serve the static files
FROM nginx:alpine
# Copy the published wwwroot folder to Nginx's html folder
COPY --from=build /app/out/wwwroot /usr/share/nginx/html
CMD ["nginx", "-g", "daemon off;"]
