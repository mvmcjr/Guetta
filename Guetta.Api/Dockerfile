FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
RUN apk add opus opus-dev libsodium-dev python3 curl ffmpeg
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
WORKDIR /src
COPY ["Guetta.Api/Guetta.Api.csproj", "Guetta.Api/"]
COPY ["Guetta.Commands/Guetta.Commands.csproj", "Guetta.Commands/"]
COPY ["Guetta.App/Guetta.App.csproj", "Guetta.App/"]
COPY ["Guetta.Abstractions/Guetta.Abstractions.csproj", "Guetta.Abstractions/"]
COPY ["Guetta.Localisation/Guetta.Localisation.csproj", "Guetta.Localisation/"]
RUN dotnet restore -a $TARGETARCH "Guetta.Api/Guetta.Api.csproj"
COPY . .
WORKDIR "/src/Guetta.Api"
RUN dotnet build -a $TARGETARCH "Guetta.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -a $TARGETARCH "Guetta.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Guetta.Api.dll"]
