# Using dotnet-trace in Azure Container Apps (testing)

First of all, the application must be deployed with a [VNET to an external Azure Container Apps environment](https://learn.microsoft.com/en-us/azure/container-apps/vnet-custom?tabs=bash%2Cazure-cli&pivots=azure-portal) to allow the application to expose [additional TCP ports](https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview#additional-tcp-ports).

To deploy the container as a sidecar, inside Azure Portal go to the Azure Container App -> Containers -> Edit and deploy.

In the "Volumes" tab, create a new "Ephemeral storage" volume.

In the "Container" tab, under "Container image" add an app container (sidecar container).

The Dockerfile in this repository can be used to create an image in any container registry and deploy from there. However, the docker-trace-container image from this repository is already available in Docker Hub can by used by setting:

| Field | Value |
|----------|----------|
| Image source  | Docker Hub or other registries  |
| Image type | Public  |
| Registry login server | docker.io |
| Image and tag | eduardovpp/dotnet-trace-container |

Set CPU cores and memory as needed and also add the following environment variable:

| Environment Variable | Source | Value |
|----------------------|--------|-------|
|  VOLUME_MOUNT_PATH  | Manual entry | /diag |

Switch to the "Volume mounts" tab and add the created ephemeral storage volume, set the mount path to "/diag" too and add the new container.

Finally create the new revision with the volume and the dotnet-trace container.

To set an additional TCP port so it's possible to send requests to the dotnet-trace container, go to Ingress -> Additional TCP ports and add the port 5000 as target and exposed port, let it accept traffict from anywhere and save. For this step it's important that the Azure Container App was created with a VNET with an external environment.

Now it should be possible to send the following requests.


```
# Lists the dotnet processes that traces can be collected from
curl http://<application_url>:5000/ps

# Collect a trace by sending a request with the PID, the file name of the trace and a duration
curl -X POST -H "Content-Type: application/json" -d '{"Command": "dotnet-trace collect -p <pid> -o <file name with .nettrace extension> --profile gc-collect --duration <in hh:mm:ss format>"}' http://<application_url>:5000/capture-trace

# Get the collected traces (stored in the volume)
curl http://<application_url>:5000/ls

# Download one of the traces
wget http://<application_url>:5000/download/<file>

# Delete one of the traces
curl -X DELETE http://<application_url>:5000/delete/<file>
```



