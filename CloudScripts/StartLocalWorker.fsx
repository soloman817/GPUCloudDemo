#load "Common.fsx"

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
open CloudScripts

let cluster = Runtime.GetHandle(config)

cluster.ShowWorkers()
cluster.ShowProcesses()

cluster.AttachLocalWorker(1, 1)

cluster.ClearAllProcesses()
