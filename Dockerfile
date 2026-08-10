# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file
COPY ["SpaceBook.sln", "./"]

# Copy project files directly from their folders
COPY ["SpaceBook.API/SpaceBook.API.csproj", "SpaceBook.API/"]
COPY ["SpaceBook.Application/SpaceBook.Application.csproj", "SpaceBook.Application/"]
COPY ["SpaceBook.Domain/SpaceBook.Domain.csproj", "SpaceBook.Domain/"]
COPY ["SpaceBook.Infrastructure/SpaceBook.Infrastructure.csproj", "SpaceBook.Infrastructure/"]

RUN dotnet restore "SpaceBook.API/SpaceBook.API.csproj"

# Copy everything else and build the project
COPY . .
WORKDIR "/src/SpaceBook.API"
RUN dotnet publish "SpaceBook.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SpaceBook.API.dll"]