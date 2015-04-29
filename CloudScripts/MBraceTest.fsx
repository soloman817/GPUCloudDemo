#I "../packages"
#load "MBrace.Azure.Standalone/MBrace.Azure.fsx"
#I "../packages/Newtonsoft.Json/lib/net45"
#I "../packages/Microsoft.Data.Edm/lib/net40"
#I "../packages/Microsoft.Data.Services.Client/lib/net40"
#I "../packages/Microsoft.Data.OData/lib/net40"
#I "../packages/System.Spatial/lib/net40"

open System
open System.IO
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

let myStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=weelasta696ad2824ae6414e;AccountKey=tbnG229Y8iPrq11IkZ4Ez5uZf4a6albo0JeMMepq0q6PjNxuunLzhMX2JxMkr8gK2plu95UU7OPoZJlTJT1DOw=="
let myServiceBusConnectionString = "Endpoint=sb://brisk-we4089696ad282.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=lV6WGAZj+IXuDugg6as+WlOlqjKmPGaaTOTGkoRwdIY="

let config =
    { Configuration.Default with
        StorageConnectionString = myStorageConnectionString
        ServiceBusConnectionString = myServiceBusConnectionString }

let cluster = Runtime.GetHandle(config)

let str = 
    cloud { return (sprintf "%A" <@ fun a -> a + 1 @>) }
    |> cluster.Run

