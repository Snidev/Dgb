# DgbTest
## DotnetGameBoy test project file

This is where all testing code is housed for the emulator itself. Most of the 
projects testing is done through test roms and/or log files which tend to be 
much too large to include in this repository. As such, they will be omitted
however links to them will be provided.

The project expects these test files to be located in the TestData directory

- [Blargg's](https://github.com/retrio/gb-test-roms) cpu_instrs roms were used 
in conjunction with [execution logs provided by Peach](https://github.com/wheremyfoodat/Gameboy-logs).
In order for these tests to function properly the .gb rom and .txt log file 
share the same name eg: 01-special.gb will be loaded alongside 01-special.txt
  