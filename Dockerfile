# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies for all projects
COPY ["SpaceBook.sln", "./"]
# Note: If your .sln file is named differently, update "SpaceBook.sln" above to match your actual solution filename.

# Copy all project files across the folder structure to restore dependencies correctly
# (Adjust these paths if your folder structure differs)
COPY ["src/SpaceBook.Api/*.csproj", "src/SpaceBook.Api/"]
COPY ["src/SpaceBook.Application/*.csproj", "src/SpaceBook.Application/"]
COPY ["src/SpaceBook.Domain/*.csproj", "src/SpaceBook.Domain/"]
COPY ["src/SpaceBook.Infrastructure/*.csproj", "src/SpaceBook.Infrastructure/"]

RUN dotnet restore "src/SpaceBook.Api/SpaceBook.Api.csproj"

# Copy everything else and build the project
COPY . .
WORKDIR "/src/src/SpaceBook.Api"
RUN dotnet publish "SpaceBook.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port Render expects (Render assigns a PORT environment variable, but 8080 is standard)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SpaceBook.Api.dll"]