COPY C:\src\ServiceStack\src\ServiceStack.Razor\bin\Release\* .
COPY C:\src\ServiceStack\src\ServiceStack.Server\bin\Release\* .
COPY C:\src\ServiceStack.Aws\src\ServiceStack.Aws\bin\Release\ServiceStack.Aws.* .

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\src\ServiceStack.Aws\bin\%BUILD%\ServiceStack.Aws.* ..\..\ServiceStack\lib
