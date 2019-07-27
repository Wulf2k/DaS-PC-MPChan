@echo off
set CWD=%~dp0
if exist bin rmdir /s /q bin
if exist dist rmdir /s /q dist
if exist packages rmdir /s /q packages
if exist merge.log del merge.log

cd %CWD%\il-repack
if exist packages rmdir /s /q packages

cd %CWD%\il-repack\cecil
if exist bil rmdir /s /q bil
if exist obj rmdir /s /q obj

cd %CWD%\il-repack\ILRepack
if exist bil rmdir /s /q bil
if exist obj rmdir /s /q obj

cd %CWD%\DaS-PC-MPChan
if exist bil rmdir /s /q bil
if exist obj rmdir /s /q obj

cd %CWD%
