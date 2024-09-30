import os
import tarfile
import shutil

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
INPUT_DIR = os.path.join(SCRIPT_DIR, '..', 'logs')

def extract_tar_gz(file_path, output_dir):
    with tarfile.open(file_path, 'r:gz') as tar:
        tar.extractall(output_dir)


def should_replace(existing_file, new_file):
    return os.path.getsize(new_file) > os.path.getsize(existing_file)


def process_tar_gz_files(input_dir):
    for file_name in os.listdir(input_dir):
        if file_name.endswith('.tar.gz'):
            parts = file_name.split('_')
            if len(parts) > 1:
                subfolder = parts[1]
                output_dir = os.path.join(input_dir, subfolder, "logs")
                os.makedirs(output_dir, exist_ok=True)
                
                file_path = os.path.join(input_dir, file_name)
                temp_output = os.path.join(output_dir, os.path.splitext(file_name)[0])
                os.makedirs(temp_output, exist_ok=True)
                
                extract_tar_gz(file_path, temp_output)
                
                for root, _, files in os.walk(temp_output):
                    for file in files:
                        source = os.path.join(root, file)
                        destination = os.path.join(output_dir, file)
                        
                        if os.path.exists(destination):
                            if should_replace(destination, source):
                                os.replace(source, destination)
                        else:
                            os.rename(source, destination)
                
                shutil.rmtree(temp_output)


if __name__ == '__main__':
    process_tar_gz_files(INPUT_DIR)
    print("Extraction complete.")
