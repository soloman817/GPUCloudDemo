module MyApp.FS.SimpleTemplate

open Alea.CUDA

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