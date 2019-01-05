#!/usr/bin/env bash

slnname=$1
template=$2

echo $slnname
#mkdir $dir
#pushd $dir

dotnet new sln -n $slnname
mkdir src
mkdir tests

pushd src
mkdir web
pushd web
dotnet new $template

dotnet add package autofac.extensions.dependencyinjection
dotnet add package Serilog.AspNetCore
dotnet add package serilog.settings.configuration
dotnet add package serilog.sinks.seq
dotnet add package serilog.sinks.console

popd
popd

pushd tests
mkdir tests
pushd tests
dotnet new xunit
dotnet add package NSubstitute
dotnet add reference ../../src/web/web.csproj
popd
popd

dotnet sln add src/web/web.csproj
dotnet sln add tests/tests/tests.csproj

popd

