#!/bin/sh
set -eu

# EF Core migrations and the application seeders initialize a new database.
exec dotnet QuizSystem.Api.dll
