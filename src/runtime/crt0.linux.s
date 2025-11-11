# =====================
# nxCRT entry point
# =====================

    .text
    .global _start
    
    .extern main

_start:                         # main entry point known to linker
    xor %ebp, %ebp              # effectively RBP = 0, mark the end of stack frames
    mov (%rsp), %edi            # get argc from the stack (zero extended to 64-bit)
    lea 8(%rsp), %rsi           # take the address of argv from the stack
    lea 16(%rsp, %rdi, 8), %rdx # take the address of envp from the stack
    xor %eax, %eax              # per abi and compat with icc
    call main                   # main(%edi, %rsi, %rdx)
    mov %eax, %edi              # move the return of main to the first arg of exit
    xor %eax, %eax              # per abi compat with icc
    call _exit                  # terminate