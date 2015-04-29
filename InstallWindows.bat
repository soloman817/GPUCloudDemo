.paket\paket.exe restore

rem xcopy /E /Y packages\Alea.IL\lib\net40\* packages\MBrace.Azure.Standalone\tools\
rem xcopy /E /Y packages\Alea.CUDA\lib\net40\* packages\MBrace.Azure.Standalone\tools\
rem xcopy /E /Y packages\Alea.CUDA.IL\lib\net40\* packages\MBrace.Azure.Standalone\tools\
xcopy /E /Y packages\Mono.Posix\lib\net40\* packages\MBrace.Azure.Standalone\tools\
xcopy /E /Y Patch\* packages\MBrace.Azure.Standalone\tools\ 