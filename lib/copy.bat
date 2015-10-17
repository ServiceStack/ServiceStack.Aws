COPY C:\src\ServiceStack\src\ServiceStack.Razor\bin\Release\* .
COPY C:\src\ServiceStack\src\ServiceStack.Server\bin\Release\* .

SET BUILD=Debug
REM SET BUILD=Release

COPY ..\src\ServiceStack.Aws\bin\%BUILD%\ServiceStack.Aws.* ..\..\ServiceStack\lib
