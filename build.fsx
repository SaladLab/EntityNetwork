#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open BuildLib

let solution = 
    initSolution
        "./EntityNetwork.sln" "Release" 
        [ { emptyProject with Name = "EntityNetwork"
                              Folder = "./core/EntityNetwork"
                              Dependencies = 
                                  [ ("protobuf-net", "")
                                    ("TrackableData", "")
                                    ("TrackableData.Protobuf", "")
                                    ("TypeAlias", "") ] }
          { emptyProject with Name = "EntityNetwork.Templates"
                              Folder = "./core/CodeGenerator-Templates"
                              Template = true
                              Dependencies = [ ("EntityNetwork", "") ] }
          { emptyProject with Name = "EntityNetwork.Unity3D"
                              Folder = "./plugins/EntityNetwork.Unity3D"
                              DefaultTarget = "net35"
                              Dependencies = [ ("EntityNetwork", "") ] } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ -> buildSolution solution

Target "Test" <| fun _ -> testSolution solution

Target "Cover" <| fun _ ->
     coverSolutionWithParams 
        (fun p -> { p with Filter = "+[EntityNetwork*]* -[*.Tests]*" })
        solution

Target "Coverity" <| fun _ -> coveritySolution solution "SaladLab/EntityNetwork"

Target "Nuget" <| fun _ ->
    createNugetPackages solution
    publishNugetPackages solution

Target "CreateNuget" <| fun _ ->
    createNugetPackages solution

Target "PublishNuget" <| fun _ ->
    publishNugetPackages solution

Target "Unity" <| fun _ -> buildUnityPackage "./core/UnityPackage"

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun _ -> None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"
  ==> "Test"

"Build" ==> "Nuget"
"Build" ==> "CreateNuget"
"Build" ==> "Cover"
"Restore" ==> "Coverity"

"Test" ==> "CI"
"Cover" ==> "CI"
"Nuget" ==> "CI"

RunTargetOrDefault "Help"
