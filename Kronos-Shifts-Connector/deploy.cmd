@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off 

echo Current Directory is %CD%

IF "%SITE_ROLE%" == "api" (
  echo API Deployment.
  deploy.api.cmd
) ELSE (
  IF "%SITE_ROLE%" == "config" (
      echo Config App Deployment.
    deploy.config.cmd
  ) ELSE (
    echo You have to set SITE_ROLE setting to either "api" or "config"
    exit /b 1
  )
)