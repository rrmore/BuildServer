:run.bat

cd  ClientGUI/bin/debug

START ClientGUI.exe

cd ../../..

cd RepoMock/bin/debug
START Repository.exe

cd ../../..

cd MBuilder/bin/debug
START MotherBuilder.exe

cd ../../..
cd TestHarness/bin/debug
START TestHarness.exe

@echo:

cd ../..