import os
import sys
from pathlib import Path
import subprocess
import git
import shutil
from datetime import datetime

# --- КОНФИГУРАЦИЯ (ОТНОСИТЕЛЬНЫЕ ПУТИ) ---
# Определяем корневую папку, где лежит этот скрипт
SCRIPT_DIR = Path(__file__).parent.absolute()

# 1. Путь к приватному SSH-ключу (лежит в .ssh/ рядом со скриптом)
PRIVATE_KEY_PATH = SCRIPT_DIR / '.ssh' / 'spt_profiles_key'

# 2. SSH-адрес вашего репозитория на GitHub (ЗАМЕНИТЕ НА СВОЙ!)
REPO_SSH_URL = 'git@github.com:fanteeek/spt-profiles.git'

# 3. Путь к папке с игровыми профилями SPT-AKI
#    Предполагается, что скрипт запускается из корневой папки игры,
#    поэтому используем относительный путь от текущей рабочей директории
GAME_PROFILES_PATH = Path.cwd() / 'SPT' / 'user' / 'profiles'

# 4. Путь для локальной копии репозитория (создадим рядом со скриптом)
LOCAL_REPO_PATH = SCRIPT_DIR / 'profiles_repo'

# --- ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ---

def print_configuration():
    # Выводит текущую конфигурацию путей для отладки.
    print("=" * 60)
    print("КОНФИГУРАЦИЯ ПУТЕЙ:")
    print(f"  Папка скрипта:      {SCRIPT_DIR}")
    print(f"  Приватный ключ:     {PRIVATE_KEY_PATH}")
    print(f"  Игровые профили:    {GAME_PROFILES_PATH}")
    print(f"  Локальный репо:     {LOCAL_REPO_PATH}")
    print(f"  Репозиторий GitHub: {REPO_SSH_URL}")
    print("=" * 60)

def check_ssh_key_exists():
    # Проверяет, существует ли файл приватного ключа по указанному пути.
    print(f"\n1. Проверяю наличие SSH-ключа...")
    if PRIVATE_KEY_PATH.exists():
        print(f"   ✓ Приватный ключ найден: {PRIVATE_KEY_PATH}")
        return True
    else:
        print(f"   ✗ ОШИБКА: Файл приватного ключа не найден!")
        print(f"     Ожидаемый путь: {PRIVATE_KEY_PATH}")
        print(f"     Убедитесь, что папка '.ssh' и файл 'spt_profiles_key' находятся в одной директории со скриптом.")
        return False

def check_game_profiles_path():
    # Проверяет, существует ли папка с игровыми профилями.
    print(f"\n2. Проверяю папку игровых профилей...")
    if GAME_PROFILES_PATH.exists():
        print(f"   ✓ Папка с профилями найдена: {GAME_PROFILES_PATH}")
        return True
    else:
        print(f"   ⚠ ПРЕДУПРЕЖДЕНИЕ: Папка с профилями не найдена.")
        print(f"     Ожидаемый путь: {GAME_PROFILES_PATH}")
        print(f"     Если вы запускаете скрипт не из корня игры, укажите путь вручную.")
        print(f"     Папка будет создана автоматически при необходимости.")
        # Не останавливаем выполнение, так как папку можно создать позже
        return False

def test_ssh_connection():
    # Тестирует SSH-соединение с GitHub, используя наш ключ.
    print(f"\n3. Тестирую SSH-соединение с GitHub...")
    
    ssh_test_command = [
        'ssh',
        '-T',
        '-i', str(PRIVATE_KEY_PATH),
        '-o', 'StrictHostKeyChecking=no',
        'git@github.com'
    ]
    
    try:
        result = subprocess.run(ssh_test_command, 
                                capture_output=True, 
                                text=True, 
                                timeout=10)
        
        if "successfully authenticated" in result.stderr.lower():
            print(f"   ✓ УСПЕХ: SSH-ключ аутентифицирован!")
            for line in result.stderr.split('\n'):
                if 'hi' in line.lower():
                    print(f"     Сообщение от GitHub: {line.strip()}")
            return True
        else:
            print(f"   ✗ ПРОБЛЕМА: Не удалось аутентифицироваться.")
            if result.stderr:
                print(f"     Вывод SSH: {result.stderr.strip()}")
            return False
            
    except subprocess.TimeoutExpired:
        print(f"   ✗ ОШИБКА: Таймаут соединения. Проверьте интернет.")
        return False
    except FileNotFoundError:
        print(f"   ✗ КРИТИЧЕСКАЯ ОШИБКА: Команда 'ssh' не найдена.")
        print(f"     Установите Git for Windows или OpenSSH клиент.")
        return False
    except Exception as e:
        print(f"   ✗ НЕИЗВЕСТНАЯ ОШИБКА: {e}")
        return False

def setup_git_environment():
    # Настраивает окружение Git для использования SSH-ключа.
    print(f"\n4. Настраиваю Git для работы с SSH-ключом...")
    
    # Устанавливаем переменную окружения для SSH
    ssh_command = f'ssh -i "{PRIVATE_KEY_PATH}" -o StrictHostKeyChecking=no'
    os.environ['GIT_SSH_COMMAND'] = ssh_command
    
    print(f"   ✓ Git SSH команда настроена")
    return True

def clone_repository():
    # Клонирует репозиторий, удаляя существующую папку
    print(f"\n5. Клонирую репозиторий...")
    
    # Удаляем папку, если она существует
    if LOCAL_REPO_PATH.exists():
        import shutil
        try:
            shutil.rmtree(LOCAL_REPO_PATH)
            print(f"   Удалена существующая папка: {LOCAL_REPO_PATH}")
        except Exception as e:
            print(f"   ✗ Ошибка удаления папки: {e}")
            return None
    
    try:
        repo = git.Repo.clone_from(REPO_SSH_URL, LOCAL_REPO_PATH)
        print(f"   ✓ Репозиторий клонирован в: {LOCAL_REPO_PATH}")
        return repo
    except git.exc.GitCommandError as e:
        print(f"   ✗ ОШИБКА клонирования: {e.stderr.strip() if e.stderr else e}")
        return None
    except Exception as e:
        print(f"   ✗ Ошибка клонирования: {e}")
        return None

def pull_repository():
    # Обновляет существующий репозиторий (будет использоваться позже)
    print(f"\n[Позже] Обновляю репозиторий...")
    
    try:
        if not LOCAL_REPO_PATH.exists():
            print(f"   ✗ Репозиторий не найден, сначала нужно клонировать")
            return None
        
        # Проверяем, что это действительно git-репозиторий
        if not (LOCAL_REPO_PATH / '.git').exists():
            print(f"   ✗ Это не git-репозиторий")
            return None
            
        repo = git.Repo(LOCAL_REPO_PATH)
        origin = repo.remote('origin')
        origin.pull()
        
        print(f"   ✓ Репозиторий обновлён")
        return repo
        
    except git.exc.GitCommandError as e:
        print(f"   ✗ ОШИБКА обновления: {e.stderr.strip() if e.stderr else e}")
        return None
    except Exception as e:
        print(f"   ✗ Ошибка обновления: {e}")
        return None

def get_repository():
    # Умная функция: проверяет, есть ли репо, и выбирает нужную операцию
    print(f"\n5. Получаю репозиторий...")
    
    if LOCAL_REPO_PATH.exists() and (LOCAL_REPO_PATH / '.git').exists():
        # Репозиторий уже существует - обновляем
        return pull_repository()
    else:
        # Репозитория нет - клонируем
        return clone_repository()

def main():
    # Основная функция, запускающая проверки.
    print("\n" + "=" * 60)
    print("SPT Profiles Sync - ФАЗА 1: Проверка конфигурации")
    print("=" * 60)
    
    # Выводим текущую конфигурацию
    print_configuration()
    
    # Проверяем ключ
    if not check_ssh_key_exists():
        sys.exit(1)
    
    # Проверяем папку с профилями (не критично, но предупредим)
    check_game_profiles_path()
    
    # Проверяем соединение
    if not test_ssh_connection():
        print("\n✗ Проверка соединения не пройдена. Исправьте ошибки.")
        sys.exit(1)
    
    # НАСТРАИВАЕМ GIT (НОВОЕ)
    if not setup_git_environment():
        print("\n✗ Не удалось настроить Git окружение.")
        sys.exit(1)
    
     # КЛОНИРУЕМ РЕПОЗИТОРИЙ (НОВОЕ)
    repo = clone_repository()
    if not repo:
        print("\n✗ Не удалось клонировать репозиторий.")
        sys.exit(1)
        
    # Если всё хорошо
    print("\n" + "=" * 60)
    print("✓ ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ УСПЕШНО!")
    print("  Относительные пути настроены корректно.")
    print("  SSH-доступ к репозиторию работает.")
    print("\n  Следующий шаг: клонирование/обновление репозитория.")
    print("=" * 60)

# Точка входа в программу
if __name__ == "__main__":
    main()