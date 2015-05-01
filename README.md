# GPUCloudDemo

This project shows how to use Alea GPU in a [m-brace cloud](http://www.m-brace.net/). 

## Setup the cloud

Before running this demo, you need setup an m-brace cloud on Azure. Please follow [the getting started guide of m-brace](http://www.m-brace.net/#try). In that guide, you first setup a cloud service bus and cloud storage service through [Brisk Engine](https://www.briskengine.com/), then it is recommended to experiment with [m-brace hands-on tutorial](https://github.com/mbraceproject/MBrace.StarterKit/archive/master.zip).

During this step, you should record two service endpoint strings for service bus and storage service. You need fill these two strings in the script of this demo.

## Setup the demo

Since the m-brace cloud worker running on Azure server doesn't have GPU supported, so you need find one or multiple machines which has nvidia GPU of at least Fermi arch, then setup the mbrace local worker on these machines:

- first double check if your machine have nvidia GPU of at least Fermi arch
- second, double check if you have a [License of Alea GPU](http://quantalea.com/licensing/) installed on your machine, for more detail of how to install Alea GPU license, you can reference [here](http://quantalea.com/static/app/tutorial/quick_start/licensing_and_deployment.html), [here](http://quantalea.com/static/app/manual/compilation-license_manager.html) and [here](http://quantalea.com/licensing/)
- before you open the demo solution, please first run `InstallWindows.bat` in the solution folder. This script will do:
  - restore packages with [Paket](https://github.com/fsprojects/Paket)
  - copy some native resources for [Alea GPU JIT compilation](http://quantalea.com/static/app/manual/compilation-jit_compilation_in_detail.html) to the local cloud worker folder
- open the solution, change the configuration to `Release`, since in the script, we reference the release build assembly
- build the solution
- open `CloudScripts/Common.fsx` file, fill your connection strings of storage and service bus, and save the file
- open `CloudScripts/StartLocalWorker.fsx`, select all and execute them in F# interactive, this will popup a cmd window that runs the local mbrace worker
- you can switch to other GPU enabled machine and start another mbrace local worker

In the mbrace local worker cmd window, after initialization, it prints `Service xxxxxxx started in xx seconds`. Now you can execute:

```
cluster.ShowWorkers()
cluster.ShowProcesses()
```

these commands will show you the current m-brace cloud worker and processes. For example, I have:

```
 Workers                                                                                                                                                                                                 

 Id                                Hostname        % CPU / Cores  % Memory / Total(MB)  Network(ul/dl : kbps)   Jobs   Process Id  Initialization Time       Heartbeat                Is active 
 --                                --------        -------------  --------------------  ---------------------   ----   ----------  -------------------       ---------                --------- 
 MBraceWorkerRole_IN_0             RD00155D4A335A    4.55 / 2       59.45 / 3583.00       897.26 / 1339.75     0 / 16        2872  2015/4/27 4:53:30 +00:00  2015/5/1 3:31:51 +00:00  True      
 bee57d4423e940308f27cdaa9e950359  MACXIANG          19.11 / 8      24.21 / 16292.00        27.94 / 15.51      0 / 1         7968  2015/5/1 3:25:40 +00:00   2015/5/1 3:31:50 +00:00  True      
 e0677cb6ec4740918d312fc2673ec465  KINGKONG          11.25 / 4      35.31 / 16246.00        26.57 / 16.71      0 / 1        12156  2015/5/1 3:26:28 +00:00   2015/5/1 3:31:50 +00:00  True      

 Processes                                                                                                   

 Name  Process Id  Status  Completed  Execution Time  Jobs  Result Type  Start Time  Completion Time 
 ----  ----------  ------  ---------  --------------  ----  -----------  ----------  --------------- 

Jobs : Active / Faulted / Completed / Total
```

This worker `MBraceWorkerRol_IN_0` is on azure server, which doesn't have GPU capability. The other two workers are my desktop machine (with GTX 580) and my laptop (with GTX 750m).

## Run the demo

Hint: before each demo running, it is suggested to reset the F# interactive session.

Note: it would be slow for the first time you run these demo scripts, cause m-brace needs to upload your assemblies (include those references) to the cloud, and all cloud worker will download them.

The demo in `CloudScripts/SimpleGPU.fsx` shows how to run GPU code (both [JIT compile](http://quantalea.com/static/app/manual/compilation-jit_compilation_in_detail.html) and [AOT compile](http://quantalea.com/static/app/manual/compilation-aot_compilation_in_detail.html)) on one GPU enabled mbrace cloud worker. Note, there is an issue currently, that m-brace cannot send quotations from FSI, so we have to program GPU module in a normal F# or C# project, then reference them in the FSI script. For more details, please read the comments in the code.

The demo in `CloudScripts/CalcPI.fsx` uses m-brace's `CloudFlow` to run a big PI calculation on the cloud. For more details, please read the comments in the code. Here is an result of one simulation:

```
val oneMillion : int = 1000000
val numCloudWorkers : int = 3
val numPoints : int = 10000000
val numStreamsPerSM : int = 10
val numRuns : int = 300
val pi : float = 3.141592096
```

and the process timing is:

```
> cluster.ShowProcesses();;

 Processes                                                                                                                                                                        

 Name                        Process Id     Status  Completed  Execution Time            Jobs           Result Type      Start Time               Completion Time         
 ----                        ----------     ------  ---------  --------------            ----           -----------      ----------               ---------------         
       2fd561bf2fcf41f9bbea7c2fc7c5a952  Completed  True       00:01:41.6153151    0 /   0 /   4 /   4  float option []  2015/5/1 4:17:11 +00:00  2015/5/1 4:18:53 +00:00 

Jobs : Active / Faulted / Completed / Total
```

## Conclusion

- Azure doesn't have GPU support. One solution is to create a private GPU cloud. m-brace cloud in this demo is composed by two components: `MBrace.Core` which provides the cloud programming model; and `MBrace.Azure` which implements cloud worker with Azure services, such as service bus and storage service. If we create private GPU cloud, we need re-implement `MBrace.Azure`, such as `MBrace.PrivateGPUCloud`, which then we need implement the cloud worker, and service bus and storage service.
- When implementing the cloud worker, we need add the JIT native resources to the worker, like what we do in the `InstallWindows.bat`. Alea GPU support Windows/Linux/MacOSX, so it has its own native resource locating system.
- Since the issue that m-brace cannot send quotations from FSI script, currently, we have to code the GPU module in normal assembly.
- A GPU module instance represents a compiled and loaded GPU module, which should live as long as possible in cloud worker. To do so, in m-brace, we can use static member, for more details, please reference [here](https://github.com/mbraceproject/MBrace.StarterKit/issues/15)
- Since Azure cloud is supposed to be homogeneous cloud, which every cloud node have same configuration. But in our CalcPI demo, we return `float option`, for the cloud worker without GPU, it returns `None`. There is other ways to specify worker, for more details, please reference [here](https://github.com/mbraceproject/MBrace.StarterKit/issues/16)
- Alea GPU has reference to `Mono.Posix`, but on Windows, it will not call it, on Linux, mono has it. So Alea GPU nuget doesn't depend on `Mono.Posix`. But if you are doing cloud scripting, you need explicitly reference `Mono.Posix` so that m-brace can compile your script.
- Related m-brace issues:
  - [Quotations cannot be sent](https://github.com/mbraceproject/MBrace.StarterKit/issues/18)
  - [Worker status not reset after long run](https://github.com/mbraceproject/MBrace.StarterKit/issues/20)
  - [If worker die, process status never been reset](https://github.com/mbraceproject/MBrace.StarterKit/issues/21)
  - [Exception of local worker when network has problem](https://github.com/mbraceproject/MBrace.StarterKit/issues/22)
  - [cannot start local worker](https://github.com/mbraceproject/MBrace.StarterKit/issues/23)
  