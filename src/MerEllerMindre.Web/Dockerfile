# syntax=docker/dockerfile:1

# .NET 11 preview is required (net11.0 + LangVersion preview for C# union types,
# pinned in global.json). No mainstream runtime ships it, so we bake the nightly
# preview SDK/runtime into the image. Tags must match the SDK in global.json; pin
# to the exact preview build if 11.0-preview ever drifts.
FROM mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview AS build
WORKDIR /src

# Restore against the SDK pin + centralized build props, then the csprojs only,
# so the restore layer caches independently of source changes.
COPY global.json Directory.Build.props NuGet.config ./
COPY src/MerEllerMindre.Domain/MerEllerMindre.Domain.csproj src/MerEllerMindre.Domain/
COPY src/MerEllerMindre.Web/MerEllerMindre.Web.csproj src/MerEllerMindre.Web/
RUN dotnet restore src/MerEllerMindre.Web/MerEllerMindre.Web.csproj

# Copy the rest and publish. The Domain csproj copies data/**/*.csv to output, so
# the question packs land in /app/publish/data/packs beside the assembly.
COPY . .
RUN dotnet publish src/MerEllerMindre.Web/MerEllerMindre.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/nightly/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MerEllerMindre.Web.dll"]
