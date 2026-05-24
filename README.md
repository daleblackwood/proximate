# Proximate

Proximate is a recording proxy for live data feeds such as racing, sports timing and other polled APIs. It watches a folder of feed configs. For each feed it polls or listens to the source, keeps compressed snapshots and logs, and serves them back over HTTP. Clients get one steady endpoint for both live data and playback of anything recorded earlier.

## Folders

| Folder | What's in it |
|---|---|
| `Proximate/` | The .NET 9 service, built on EmbedIO |
| `Proximate/src/core` | Endpoint base, access control, config and logging |
| `Proximate/src/endpoints` | HTTP endpoints. The route comes from the class name, so `FeedsEndpoint` serves `/feeds` |
| `Proximate/src/proxies` | Consumers for REST, OAuth, GraphQL and TCP sources |
| `Proximate/src/services` | Recorder, snapshot log, auth and user services |
| `Proximate/src/utils` | Snapshotting, diffing, compression, log reading and timing helpers |
| `Proximate/config` | One `.toml` per feed, picked up and reloaded while running. Files starting with `.` are skipped |
| `Proximate/public/viewer` | Browser viewer for feeds and logs, served at `/viewer` |
| `Proximate.Tests/` | xUnit tests, with sample configs in `data/` |
| `Proximate.Deploy/` | Pulumi stacks for AWS: `ProximateNetStack` runs plain .NET on an instance, `ProximateDockerStack` runs it in Docker |
| `scripts/` | Deployment, config push, log pull and remote-access helpers, plus TCP test tools |
| `build` | Wrapper for the Docker image and Pulumi deploy tasks |

## Feed config

Each file in `Proximate/config` defines one feed. The file name becomes the feed's id.

```toml
api_url = "https://example.com/feed"
poll_interval = "1:00"      # mm:ss or hh:mm:ss, how often to poll
key_interval = "30:00"      # how often to store a full keyframe
```

OAuth feeds also take `api_oauth_url`, `api_client_id` and `api_client_secret`. Set `api_oauth_grant = "password"` for APIs that take a username and password instead of client credentials. `${VAR}` in credentials is read from the environment. GraphQL feeds take `api_query`.

The main example is `config/openf1.toml`, which polls [OpenF1](https://openf1.org) using `OPENF1_USERNAME` and `OPENF1_PASSWORD`. Without them it still gets OpenF1's free historical data. `config/.oauth-demo.toml` is a disabled client-credentials example against Duende's public demo server.

## Testing

Requires the .NET 9 SDK.

```bash
dotnet test Proximate.Tests
```

The OAuth tests need network access. The OpenF1 test runs only when `OPENF1_USERNAME` and `OPENF1_PASSWORD` are set and is skipped otherwise. The client-credentials test uses Duende's public demo server.

## Running

Locally:

```bash
PROX_PORT=8080 dotnet run --project Proximate
```

Then open http://localhost:8080/viewer. `PROX_PORT` defaults to 80. `ASPNETCORE_ENVIRONMENT` can be `Development`, `Testing` or `Production`.

In Docker, on port 80:

```bash
./build image_run
```

## Deploying

Deployment uses Pulumi and needs the AWS CLI. Put `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` and `AWS_REGION` in `Proximate.Deploy/artefacts/dev.env`, which is git-ignored.

```bash
./build publish     # build, deploy the stack, push configs
./build pull_down   # pull logs, then destroy the stack
```
