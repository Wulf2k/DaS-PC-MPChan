#!/bin/env python3
import os
import os.path
import sys
import subprocess
import textwrap

fname = sys.argv[1]

os.system("gcc -x assembler -c {} -m32 -o tmp.o".format(fname))
os.system("objdump -M intel -d tmp.o")
bin = subprocess.check_output("objdump -s tmp.o | tail -n +5 | xxd -r ", shell=True)
os.unlink("tmp.o")

print("\n"*2)
out = ", ".join('&H{:02X}'.format(x) for x in bin)
print("\n".join(textwrap.wrap(out, 6*16)))
