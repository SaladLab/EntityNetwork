pushd %~dp0

SET SRC=..\EntityNetwork.Net35\bin\Release
SET SRC_UNITY3D=..\..\plugins\EntityNetwork.Unity3D\bin\Release
SET DST=.\Assets\Middlewares\EntityNetwork
SET PDB2MDB=..\..\tools\unity3d\pdb2mdb.exe

%PDB2MDB% "%SRC%\EntityNetwork.dll"
%PDB2MDB% "%SRC_UNITY3D%\EntityNetwork.Unity3D.dll"

COPY /Y "%SRC%\EntityNetwork.dll" %DST%
COPY /Y "%SRC%\EntityNetwork.dll.mdb" %DST%
COPY /Y "%SRC_UNITY3D%\EntityNetwork.Unity3D.dll" %DST%
COPY /Y "%SRC_UNITY3D%\EntityNetwork.Unity3D.dll.mdb" %DST%

popd
