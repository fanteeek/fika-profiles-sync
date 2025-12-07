import os
import shutil
import time
import stat
from pathlib import Path
from typing import List, Optional
from datetime import datetime
from utils.logger import Logger

logger = Logger()

def remove_readonly(func, path, _):
    try:
        os.chmod(path, stat.S_IWRITE)
        func(path)
    except Exception:
        pass

class FileManager:
    def __init__(self):
        pass
    
    def force_delete_folder(self, folder_path: Path) -> bool:
        if not folder_path.exists():
            return True
        
        max_attempts = 3
        for attempt in range(max_attempts):
            try:
                shutil.rmtree(folder_path, onerror=remove_readonly)
                if not folder_path.exists():
                    return True
                
            except Exception as e:
                if attempt == max_attempts -1:
                    logger.log('DEBUG', f'Не удалось удалить {folder_path}: {e}', 'error')
                    
                time.sleep(0.5)
        
        return not folder_path.exists()
    
    def check_game_profiles_path(self, profiles_path: Path) -> bool:
        logger.log('DEBUG', f'Проверяю папку: {profiles_path}')
        
        if profiles_path.exists():
            json_count = len(list(profiles_path.glob('*.json')))
            logger.log('PATH', f'Папка SPT Profiles найдена ({json_count} профилей)', 'ok')
            return True
        
        logger.log('PATH', f'Папка не найдена: {profiles_path}', 'warn')
        logger.log('PATH', 'Будет создана при необходимости')
        return True
    
    def create_backup(self, local_file: Path, backup_base_dir: Path) -> Optional[Path]:
        try:
            backup_dir = backup_base_dir / datetime.now().strftime('%Y%m%d_%H%M%S')
            backup_dir.mkdir(parents=True, exist_ok=True)
            
            backup_file = backup_dir / local_file.name
            shutil.copy2(local_file, backup_file)
            
            return backup_file
        except Exception as e:
            logger.log('BACKUP', f'Ошибка создания бэкапа {local_file.name}: {e}', 'warn')
            return None
    
    def cleanup_temp_files(self, temp_folders: List[Path]) -> None:
        for folder in temp_folders:
            if folder.exists():
                if self.force_delete_folder(folder):
                    logger.log('DEBUG', f'Удалено: {folder.name}', 'ok')
                else:
                    logger.log('DEBUG', f'Не удалось удалить: {folder.name}', 'warn')