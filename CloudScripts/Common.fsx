#I "../packages"
#load "MBrace.Azure.Standalone/MBrace.Azure.fsx"
#r "Streams/lib/net45/Streams.Core.dll"
#r "MBrace.Flow/lib/net45/MBrace.Flow.dll"
#I "../packages/Newtonsoft.Json/lib/net45"
#I "../packages/Microsoft.Data.Edm/lib/net40"
#I "../packages/Microsoft.Data.Services.Client/lib/net40"
#I "../packages/Microsoft.Data.OData/lib/net40"
#I "../packages/System.Spatial/lib/net40"
#r "System.Configuration"
#r "Alea.IL/lib/net40/Alea.IL.dll"
#r "Alea.CUDA/lib/net40/Alea.CUDA.dll"
#r "Alea.CUDA.IL/lib/net40/Alea.CUDA.IL.dll"
#r "Alea.CUDA.Unbound/lib/net40/Alea.CUDA.Unbound.dll"

namespace CloudScripts

[<AutoOpen>]
module Common =

    open System
    open System.IO
    open System.Net
    open MBrace
    open MBrace.Azure
    open MBrace.Azure.Client
    open MBrace.Azure.Runtime
    open MBrace.Workflows
    open MBrace.Flow
    open Alea.CUDA
    open Alea.CUDA.Utilities
    open Alea.CUDA.Unbound

    let myStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=weelasta696ad2824ae6414e;AccountKey=tbnG229Y8iPrq11IkZ4Ez5uZf4a6albo0JeMMepq0q6PjNxuunLzhMX2JxMkr8gK2plu95UU7OPoZJlTJT1DOw=="
    let myServiceBusConnectionString = "Endpoint=sb://brisk-we4089696ad282.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=lV6WGAZj+IXuDugg6as+WlOlqjKmPGaaTOTGkoRwdIY="

    let config =
        { Configuration.Default with
            StorageConnectionString = myStorageConnectionString
            ServiceBusConnectionString = myServiceBusConnectionString }

    let isGPUEnabled =
        cloud {
            let! w = Cloud.CurrentWorker
            try Device.Default |> ignore; return Some w
            with _ -> return None }

    let gpuWorker (cluster:Runtime) =
        let gpuWorkers =
            cloud { 
                let! results = Cloud.ParallelEverywhere isGPUEnabled
                return results |> Array.choose id }
            |> cluster.Run
        if gpuWorkers.Length = 0 then failwith "No GPU enabled worker found!"
        else
            printfn "Found these gpu enabled workers (total %d workers):" gpuWorkers.Length
            for gpuWorker in gpuWorkers do
                printfn "--> %s" ((gpuWorker :?> WorkerRef).Hostname)
            let gpuWorker = gpuWorkers.[0]
            printfn "We choose worker %A." ((gpuWorker :?> WorkerRef).Hostname)
            gpuWorker

    let gpuRun (cluster:Runtime) gpuWorker (computation:Cloud<'T>) : 'T =
        let workflow = cloud { return! Cloud.StartAsCloudTask(computation, target = gpuWorker) }
        cluster.Run(workflow).Result

