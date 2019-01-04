#!/usr/bin/env bash

dir=$1

echo $dir
mkdir $dir
pushd $dir

dotnet new sln
mkdir src
mkdir tests

pushd src
mkdir web
pushd web
dotnet new mvc
popd
popd

pushd tests
dotnet new xunit
popd

dotnet sln add src/web/web.csproj
dotnet sln add tests/tests.csproj

popd
