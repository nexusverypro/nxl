# =====================
# nxCRT entry point
# =====================

    .text
    .global _start

    .extern GetCommandLineW
    .extern CommandLineToArgvW
    .extern LocalFree
    .extern main
    .extern ExitProcess

_start:                                 # main entry point known to linker
    xor    %rbp, %rbp                   # effectively RBP = 0, mark end of stack frames
    sub    $40, %rsp                    # allocate 32 bytes shadow space + 8 for 16-byte alignment
    call   GetCommandLineW              # -> RAX = LPWSTR command line string
    mov    %rax, %rcx                   # RCX = lpCmdLine (first arg)
    lea    32(%rsp), %rdx               # RDX = &argc (second arg)
    call   CommandLineToArgvW           # -> RAX = LPWSTR* argv, argc stored at [RSP+32]
    mov    %rax, %rbx                   # save argv pointer (LPWSTR*)
    mov    32(%rsp), %eax               # load argc (32-bit int)
    mov    %eax, %ecx                   # RCX = argc (first arg to wmain)
    mov    %rbx, %rdx                   # RDX = argv (second arg to wmain)
    xor    %r8d, %r8d                   # clear R8 (third arg unused, per ABI)
    call   main                         # call main(argc, argv)
    mov    %eax, %ecx                   # move return of wmain to ECX (exit code)
    mov    %rbx, %rcx                   # RCX = argv pointer (first arg to LocalFree)
    call   LocalFree                    # free memory allocated by CommandLineToArgvW
    call   ExitProcess                  # terminate process with return code in ECX
