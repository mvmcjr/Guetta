FROM alpine:3.23 AS ffmpeg-base
RUN apk update && apk add wget
RUN wget https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz
RUN mkdir /ffmpeg
RUN tar -C /ffmpeg -xvf ffmpeg-release-amd64-static.tar.xz --strip-components=1

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY . .

# Fetch libdave native binaries
RUN apt-get update && apt-get install -y unzip && apt-get clean
RUN bash Guetta.Natives/fetch-natives.sh

WORKDIR "/src/Guetta"
RUN dotnet publish "Guetta.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final

RUN apt-get update && apt-get install python3 curl -y && apt-get clean && apt-get autoremove -y

WORKDIR /app
COPY --from=build /app/publish .
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp

COPY --from=ffmpeg-base /ffmpeg/ffmpeg /usr/local/bin/
COPY --from=ffmpeg-base /ffmpeg/ffprobe /usr/local/bin/
RUN chmod a+rx /usr/local/bin/ffmpeg
ENTRYPOINT ["dotnet", "Guetta.dll"]
