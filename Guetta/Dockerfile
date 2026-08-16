FROM --platform=$BUILDPLATFORM alpine:3.24 AS ffmpeg-base
ARG TARGETARCH
RUN apk update && apk add --no-cache curl tar xz
RUN set -eu; \
    case "$TARGETARCH" in \
      amd64) FFMPEG_ARCH=linux64 ;; \
      arm64) FFMPEG_ARCH=linuxarm64 ;; \
      *) echo "Unsupported arch: $TARGETARCH" >&2; exit 1 ;; \
    esac; \
    curl -fL -o /tmp/ffmpeg.tar.xz "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-${FFMPEG_ARCH}-gpl.tar.xz"; \
    mkdir -p /ffmpeg-extract /ffmpeg; \
    tar -C /ffmpeg-extract -xf /tmp/ffmpeg.tar.xz; \
    mv /ffmpeg-extract/*/bin/ffmpeg /ffmpeg-extract/*/bin/ffprobe /ffmpeg/

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
RUN chmod a+rx /usr/local/bin/ffmpeg /usr/local/bin/ffprobe
ENTRYPOINT ["dotnet", "Guetta.dll"]
