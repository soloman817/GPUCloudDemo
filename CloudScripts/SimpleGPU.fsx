#load "Common.fsx"
#r "../MyApp.FS/bin/Release/MyApp.FS.exe"

open System
open System.IO
open System.Threading
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open Alea.CUDA
open Alea.CUDA.Utilities
open Alea.CUDA.Unbound
open MyApp.FS
open CloudScripts

let cluster = Runtime.GetHandle(config)
let gpuWorker = gpuWorker cluster

//let remoteGPU = cloud { return Worker.Default.ToString() } |> gpuRun cluster gpuWorker
//
//let remoteResourcePath = cloud { return Settings.Instance.Resource.Path } |> gpuRun cluster gpuWorker

module GPUModuleJIT =
    
    let gpuAddOneOnRemoteNonStatic (inputs:int[]) =
        cloud {
            let gpuModule = new SimpleModule.JITModule(GPUModuleTarget.DefaultWorker)
            let result = gpuModule.AddOne(inputs)
            gpuModule.Dispose()
            return result }
        |> gpuRun cluster gpuWorker

//GPUModuleJIT.gpuAddOneOnRemoteNonStatic [| 1; 2; 3; 4; 5 |]

module GPUModuleAOT =
    
    let gpuAddOneOnRemoteNonStatic (inputs:int[]) =
        doJob cluster <| fun _ ->
            let gpuModule = SimpleModule.AOTModule.Default
            gpuModule.AddOne([| 1; 2; 3; 4; 5 |])

//        cloud {
//            let gpuModule = new SimpleModule.AOTModule(GPUModuleTarget.DefaultWorker)
//            let result = gpuModule.AddOne(inputs)
//            gpuModule.Dispose()
//            return result }
//        |> gpuRun cluster gpuWorker

GPUModuleAOT.gpuAddOneOnRemoteNonStatic [| 1; 2; 3; 4; 5 |]

// This doesn't work now because it cannot send fsi quotation to remote
module GPUTemplateScripting =

    let template = cuda {
        let! kernel =
            <@ fun (data:deviceptr<int>) ->
                let tid = threadIdx.x
                data.[tid] <- data.[tid] + 1 @>
            |> Compiler.DefineKernel

        return Entry(fun program ->
            let worker = program.Worker
            let kernel = program.Apply kernel

            let run (data:int[]) =
                if data.Length > 512 then failwith "data.Length should <= 512, this is a simple test."
                use data = worker.Malloc(data)
                let lp = LaunchParam(1, data.Length)
                kernel.Launch lp data.Ptr
                data.Gather()

            run ) }

    let gpuAddOneOnLocal (data:int[]) =
        use program = Worker.Default.LoadProgram(template)
        program.Run data

    let gpuAddOneOnRemote (data:int[]) =
        cloud { return gpuAddOneOnLocal data } |> gpuRun cluster gpuWorker

// This doesn't work now because it cannot send fsi quotation to remote
//GPUScripting.gpuAddOneOnRemote [| 1; 2; 3; 4; 5 |]

module GPUTemplate =
//    let gpuAddOneOnLocal (data:int[]) =
//        use program = Worker.Default.LoadProgram(SimpleTemplate.template)
//        program.Run data

    let gpuAddOneOnRemote (data:int[]) =
        cloud { return SimpleTemplate.gpuAddOneOnLocal data } |> gpuRun cluster gpuWorker

// This also has strange error
//GPUTemplate.gpuAddOneOnRemote [| 1; 2; 3; 4; 5 |]    



