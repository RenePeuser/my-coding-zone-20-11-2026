// Every Assert.That.CodeRule call builds its own engine from its own files, so all tests run in parallel.

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
