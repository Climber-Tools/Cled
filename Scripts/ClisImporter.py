"""Imports the models from the Clis project, generating their preview animation and image in the process.

Note that this script expects to be in Scripts/ folder of the Unity project.
It also exports the holds to ../Models/Holds/.
"""

import os
import argparse
import shutil
import hashlib
from subprocess import Popen
from yaml import load, dump
from yaml import CLoader as Loader, CDumper as Dumper

def get_file_hashsum(path, characters=12):
    """Return the hashsum of the file contents."""
    with open(path) as f:
        return hashlib.sha256(f.read().encode("utf-8")).hexdigest()[:12]


cwd = os.getcwd()

os.chdir(os.path.dirname(os.path.abspath(__file__)))

parser = argparse.ArgumentParser()

parser.add_argument(
    "input",
    help="The path to the models/ folder generated by Clis.",
)

parser.add_argument(
    "output",
    help="The path to which the holds should be copied.",
)

arguments = parser.parse_args()

if not os.path.exists(arguments.output):
    os.makedirs(arguments.output)
else:
    print("Destination folder exists, not copying.")
    quit()

shutil.copy(os.path.join(arguments.input, "holds.yaml"), os.path.join(arguments.output, "holds.yaml"))

# read the YAML and copy the contents of the folders
with open(os.path.join(arguments.input, "holds.yaml")) as f:
    data = load(f.read(), Loader=Loader)

for key in data:
    # find the folder with the key
    for file in os.listdir(arguments.input):
        objfile = os.path.join(arguments.input, file, "model.obj")

        if os.path.exists(objfile) and get_file_hashsum(objfile) == key:
            break

    hold_folder = os.path.join(arguments.input, file)

    # copy over the model and its texture files
    for file in ["model.obj", "model.mtl", "model.jpg"]:
        _, ext = os.path.splitext(file)

        src_file = os.path.join(hold_folder, file)

        dest_file_stub = os.path.join(arguments.output, key)
        dest_file = dest_file_stub + ext

        shutil.copy(src_file, dest_file)

        if ext == ".obj":
            # generate the preview for the hold
            Popen(["python3", "GenerateHoldPreview.py", src_file, dest_file_stub]).communicate()

        # replace the path to the texture in the mtl file
        if ext == ".mtl":
            with open(dest_file) as f:
                contents = f.read()

            contents = contents.replace("model.jpg", key + ".jpg")

            with open(dest_file, "w") as f:
                f.write(contents)

# TODO: warn about models that were not found
# TODO: only copy files that were not in already
# TODO: warn about models that were found but should not have been
