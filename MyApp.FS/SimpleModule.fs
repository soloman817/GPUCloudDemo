module MyApp.FS.SimpleModule

open System
open Alea.CUDA

type JITModule(target) =
    inherit GPUModule(target)

    let uuid = Guid.NewGuid()
    do printfn "constructing JITModule(%A)..." uuid

    static let defaultInstance = Lazy.Create <| fun _ -> new JITModule(GPUModuleTarget.DefaultWorker)
    static member Default = defaultInstance.Value

    [<Kernel;ReflectedDefinition>]
    member this.Kernel (data:deviceptr<int>) =
        let tid = threadIdx.x
        data.[tid] <- data.[tid] + 1

    member this.AddOne(inputs:int[]) =
        if inputs.Length > 512 then failwith "inputs.Length should <= 512, since this is just a simple test."
        use data = this.GPUWorker.Malloc(inputs)
        let lp = LaunchParam(1, inputs.Length)
        this.GPULaunch <@ this.Kernel @> lp data.Ptr
        data.Gather()

[<AOTCompile(AOTOnly = true)>]
type AOTModule(target) =
    inherit GPUModule(target)

    let uuid = Guid.NewGuid()
    do printfn "constructing AOTModule(%A)..." uuid

    static let defaultInstance = Lazy.Create <| fun _ -> new AOTModule(GPUModuleTarget.DefaultWorker)
    static member Default = defaultInstance.Value

    [<Kernel;ReflectedDefinition>]
    member this.Kernel (data:deviceptr<int>) =
        let tid = threadIdx.x
        data.[tid] <- data.[tid] + 1

    member this.AddOne(inputs:int[]) =
        if inputs.Length > 512 then failwith "inputs.Length should <= 512, since this is just a simple test."
        use data = this.GPUWorker.Malloc(inputs)
        let lp = LaunchParam(1, inputs.Length)
        this.GPULaunch <@ this.Kernel @> lp data.Ptr
        data.Gather()

