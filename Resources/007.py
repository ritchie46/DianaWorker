import subprocess
import psutil
import sys

for i in range(100):
    print(sys.argv)

# C:\Program Files\Diana Dev\bin
# C:\Program Files\Diana 10.0\bin
# C:\Program Files\Diana 10.1\bin

process = subprocess.Popen([r"C:\Progra~1\Diana 10.1\bin\diana", "-m", r"C:\Users\vik\Desktop\leeg\iter_part_lin_min_12",
                            r"C:\Users\vik\Desktop\leeg\iter_part_lin_min_12.ff"], stdout=subprocess.PIPE)


def kill_proc_tree(pid, including_parent=True):
    parent = psutil.Process(pid)
    print("Parent pid: %s" % parent)
    children = parent.children(recursive=True)
    for child in children:
        print("Killing child: %s" % child)
        child.kill()

    psutil.wait_procs(children, timeout=5)
    if including_parent:
        parent.kill()
        parent.wait(5)


# import time
# print("sleep 20s")
# time.sleep(30)
#
# print("sleep another 30s")
# time.sleep(30)
# print("kill")
# kill_proc_tree(process.pid, including_parent=False)





