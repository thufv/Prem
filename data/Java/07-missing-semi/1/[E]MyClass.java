class MyClass {
    void f() {
        if (i > j)
            max = i // Error, missing semicolon
        else
            max = j;
    }
}