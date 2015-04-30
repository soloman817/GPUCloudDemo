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

let createParams (numPoints:int) (numStreamsPerSM:int) (numRuns:int) : CalcPI.CalcParam[] =
    let rng = Random()
    Array.init numRuns (fun _ ->
        let seed = rng.Next() |> uint32
        let getRandomXorshift7 numStreams numDimensions = Rng.XorShift7.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, seed) :> Rng.IRandom<float>
        let getRandomMrg32k3a  numStreams numDimensions = Rng.Mrg32k3a.CUDA.DefaultUniformRandomModuleF64.Default.Create(numStreams, numDimensions, seed) :> Rng.IRandom<float>
        let getRandom =
            match rng.Next(2) with
            | 0 -> getRandomXorshift7
            | _ -> getRandomMrg32k3a
        { NumPoints = numPoints; NumStreamsPerSM = numStreamsPerSM; GetRandom = getRandom } )

let oneMillion = 1000000
let numCloudWorkers = cluster.GetWorkers(showInactive = false) |> Array.ofSeq

let numPoints = oneMillion
let numStreamsPerSM = 5
let numRuns = numCloudWorkers.Length * 1000

let pis = 
    createParams numPoints numStreamsPerSM numRuns
    |> CloudFlow.ofArray
    |> CloudFlow.map CalcPI.calcPI
    |> CloudFlow.toArray
    |> cluster.Run
    |> Array.choose id
    |> Array.average

printfn "PI = %A" pis
