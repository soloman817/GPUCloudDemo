#load "Common.fsx"
#r "../MyApp.FS/bin/Release/MyApp.FS.exe"

open System
open System.IO
open System.Threading
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Workflows
open MBrace.Flow
open Alea.CUDA
open Alea.CUDA.Utilities
open Alea.CUDA.Unbound
open MyApp.FS
open CloudScripts

let cluster = Runtime.GetHandle(config)
cluster.ClearAllProcesses()

// a function to create a list of calcPI task parameter. We randomly select the seed and rng.
let createParams (numPoints:int) (numStreamsPerSM:int) (numRuns:int) : CalcPI.CalcParam[] =
    let rng = Random()
    Array.init numRuns (fun taskId ->
        let seed = rng.Next() |> uint32
        let getRandomXorshift7 numStreams numDimensions = Rng.XorShift7.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, seed) :> Rng.IRandom<float>
        let getRandomMrg32k3a  numStreams numDimensions = Rng.Mrg32k3a.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, seed) :> Rng.IRandom<float>
        let getRandom =
            match rng.Next(2) with
            | 0 -> getRandomXorshift7
            | _ -> getRandomMrg32k3a
        { TaskId = taskId; NumPoints = numPoints; NumStreamsPerSM = numStreamsPerSM; GetRandom = getRandom } )

let oneMillion = 1000000
let numCloudWorkers = (cluster.GetWorkers(showInactive = false) |> Array.ofSeq).Length

let numPoints = oneMillion * 10
let numStreamsPerSM = 10
let numRuns = numCloudWorkers * 100
//let numRuns = numCloudWorkers * 2000

// this is the cloud workflow, we have a big question (numRuns task, each task will generate many 
// random streams, and calc PI through these random numbers). CloudFlow.map will map these tasks
// to avaialbe cloud workers. Because not all cloud worker has GPU, so we return float option.
// at last, we choose the results and reduce them by mean.
let pi = 
    createParams numPoints numStreamsPerSM numRuns
    |> CloudFlow.ofArray
    |> CloudFlow.map CalcPI.calcPI
    |> CloudFlow.toArray
    |> cluster.Run
    |> Array.choose id
    |> Array.average

printfn "PI = %A" pi
