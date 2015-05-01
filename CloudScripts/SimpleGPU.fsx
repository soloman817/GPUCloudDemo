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

// you can run these cloud computation to get remote gpu and alea gpu information
//let remoteGPU = cloud { return Worker.Default.ToString() } |> gpuRun cluster gpuWorker
//let remoteResourcePath = cloud { return Settings.Instance.Resource.Path } |> gpuRun cluster gpuWorker

// The GPUModule (without AOT compile) is defined in a normal assembly, and 
// note, we use JITModule.Default, which is a lazy singleton instance, live 
// in remote cloud worker. Since module should live as long as possible, we 
// use this singleton style to use the module (which is loaded in the default worker)
// for multiple GPU devices, you need create your own static container instance.
module GPUModuleJIT =
    
    let gpuAddOneOnRemote (inputs:int[]) =
        cloud { return SimpleModule.JITModule.Default.AddOne(inputs) }
        |> gpuRun cluster gpuWorker

GPUModuleJIT.gpuAddOneOnRemote [| 1; 2; 3; 4; 5 |]

// this test is similar, just the module is AOT compiled on local machine.
module GPUModuleAOT =
    
    let gpuAddOneOnRemote (inputs:int[]) =
        cloud { return SimpleModule.AOTModule.Default.AddOne(inputs) }
        |> gpuRun cluster gpuWorker

GPUModuleAOT.gpuAddOneOnRemote [| 1; 2; 3; 4; 5 |]

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

// If the gpu template is created in a normal assembly, then it works
module GPUTemplate =
    let gpuAddOneOnLocal (data:int[]) =
        use program = Worker.Default.LoadProgram(SimpleTemplate.template)
        program.Run data

    let gpuAddOneOnRemote (data:int[]) =
        cloud { return gpuAddOneOnLocal data } |> gpuRun cluster gpuWorker

GPUTemplate.gpuAddOneOnRemote [| 1; 2; 3; 4; 5 |]    



