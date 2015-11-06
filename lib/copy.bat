COPY C:\src\ServiceStack\src\ServiceStack.Razor\bin\Release\* .
COPY C:\src\ServiceStack\src\ServiceStack.Server\bin\Release\* .
COPY C:\src\ServiceStack\src\ServiceStack.Authentication.OAuth2\bin\Release\ServiceStack.Authentication.OAuth2.* .
COPY C:\src\ServiceStack\src\ServiceStack.Authentication.OpenId\bin\Release\ServiceStack.Authentication.OpenId.* .
COPY C:\src\ServiceStack.Aws\src\ServiceStack.Aws\bin\Release\ServiceStack.Aws.* .
REM COPY C:\src\ServiceStack.Aws\src\ServiceStack.Aws\bin\Debug\ServiceStack.Aws.* .

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\src\ServiceStack.Aws\bin\%BUILD%\ServiceStack.Aws.* ..\..\ServiceStack\lib
