#!/bin/bash

# This script regenerates all Entity Framework Core models and the DbContext from the current SQL Server database schema.
# It deletes all existing model classes (except custom files), removes the old DbContext, and runs the EF Core scaffold command
# to generate up-to-date C# models and context using data annotations. Use this after any database schema change to keep the code in sync.

echo "Cleaning previous scaffolded models..."

find Models -type f -name '*.cs' -delete

rm -f Data/GlucoTrackDBContext.cs

echo "Running scaffold..."

dotnet ef dbcontext scaffold \
  "Server=localhost,1433;Database=GlucoTrackDB;User Id=sa;Password=baseBase100!;TrustServerCertificate=True;" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --startup-project . \
  --context GlucoTrackDBContext \
  --output-dir Models \
  --context-dir Data \
  --data-annotations \
  --use-database-names \
  --no-onconfiguring \
  --no-pluralize \
  --force \
  --no-build
