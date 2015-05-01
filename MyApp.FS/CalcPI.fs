module MyApp.FS.CalcPI

open System
open Alea.CUDA
open Alea.CUDA.Unbound

// a gpu kernel, use [method-based way](http://quantalea.com/static/app/manual/compilation-method_based_gpu_coding.html)
[<ReflectedDefinition;AOTCompile(AOTOnly = true)>]
let kernelCountInside (pointsX:deviceptr<float>) (pointsY:deviceptr<float>) (numPoints:int) (numPointsInside:deviceptr<int>) =
    let start = blockIdx.x * blockDim.x + threadIdx.x
    let stride = gridDim.x * blockDim.x
    let mutable i = start
    while i < numPoints do
        let x = pointsX.[i]
        let y = pointsY.[i]
        numPointsInside.[i] <- if sqrt (x*x + y*y) <= 1.0 then 1 else 0
        i <- i + stride

// in this demo, we use default gpu worker, so we check if it is available
let canDoGPUCalc =
    Lazy.Create <| fun _ ->
        try Device.Default |> ignore; true
        with _ -> false

// parameters of one calc task
type CalcParam =
    { TaskId : int
      NumPoints : int
      NumStreamsPerSM : int
      GetRandom : int -> int -> Rng.IRandom<float> }

let calcPI (param:CalcParam) =
    // if no gpu, we reutrn none
    if canDoGPUCalc.Value then
        let worker = Worker.Default
        // we switch to the gpu worker thread to do the job
        worker.Eval <| fun _ ->
            let numPoints = param.NumPoints
            let numStreamsPerSM = param.NumStreamsPerSM
            let numSMs = worker.Device.Attributes.MULTIPROCESSOR_COUNT
            let numStreams = numStreamsPerSM * numSMs
            let numDimensions = 2

            let random = param.GetRandom numStreams numDimensions
            use reduce = DeviceSumModuleI32.Default.Create(numPoints)
            use points = random.AllocCUDAStreamBuffer(numPoints)
            use numPointsInside = worker.Malloc<int>(numPoints)
            let pointsX = points.Ptr
            let pointsY = points.Ptr + numPoints
            let lp = LaunchParam(numSMs * 8, 256)

            printfn "Task #.%d : Random(%s) Streams(%d) Points(%d)" param.TaskId (random.GetType().Namespace) numStreams numPoints

            // run on all random streams
            [| 0..numStreams-1 |]
            |> Array.map (fun streamId ->
                random.Fill(0, numPoints, points)
                worker.Launch <@ kernelCountInside @> lp pointsX pointsY numPoints numPointsInside.Ptr
                let numPointsInside = reduce.Reduce(numPointsInside.Ptr, numPoints)
                4.0 * (float numPointsInside) / (float numPoints) )
            |> Array.average
            |> Some

    else None

let test() =
    let numPoints = 1000000
    let numStreamsPerSM = 2
    let getRandomXorshift7 numStreams numDimensions = Rng.XorShift7.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, 42u) :> Rng.IRandom<float>
    let getRandomMrg32k3a  numStreams numDimensions = Rng.Mrg32k3a.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, 42u) :> Rng.IRandom<float>

    { TaskId = 0; NumPoints = numPoints; NumStreamsPerSM = numStreamsPerSM; GetRandom = getRandomXorshift7 }
    |> calcPI |> printfn "pi=%A"

    { TaskId = 0; NumPoints = numPoints; NumStreamsPerSM = numStreamsPerSM; GetRandom = getRandomMrg32k3a }
    |> calcPI |> printfn "pi=%A"