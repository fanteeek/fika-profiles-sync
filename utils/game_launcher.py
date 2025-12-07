import sys
import subprocess
import time
import socket
import json
import re
from pathlib import Path
from utils.logger import Logger

logger = Logger()

class GameLauncher:
    '''Запуск игры в одном окне с выходом по Enter.'''
    
    def __init__(self, config):
        self.config = config
        self.server_process = None
        self.launcher_process = None
        
        # Значения по умолчанию
        self.target_ip = '127.0.0.1' 
        self.port = 6969
    
    def _extract_values_from_text(self, text: str) -> dict:
        """Ищет port и ip в тексте через RegEx."""
        result = {}
        port_match = re.search(r'"port"\s*:\s*(\d+)', text)
        if port_match:
            try:
                result['port'] = int(port_match.group(1))
            except ValueError:
                pass
        
        ip_match = re.search(r'"ip"\s*:\s*"([^"]+)"', text)
        if ip_match:
            result['ip'] = ip_match.group(1)
            
        return result

    def _load_server_config(self):
        """Определяет IP и Порт сервера."""
        game_root = self.config.SPT_SERVER_PATH.parent
        
        # 1. Fika
        fika_config = game_root / 'user' / 'mods' / 'fika-server' / 'assets' / 'configs' / 'fika.jsonc'
        if fika_config.exists():
            try:
                with open(fika_config, 'r', encoding='utf-8') as f:
                    values = self._extract_values_from_text(f.read())
                
                if 'port' in values: self.port = values['port']
                if 'ip' in values:
                    raw = values['ip']
                    self.target_ip = '127.0.0.1' if raw == '0.0.0.0' else raw
                logger.log('GAME', f'Конфиг Fika: {self.target_ip}:{self.port}', 'ok')
                return
            except Exception:
                pass

        # 2. SPT
        possible_paths = [
            game_root / 'SPT_Data' / 'Server' / 'configs' / 'http.json',
            game_root / 'Aki_Data' / 'Server' / 'configs' / 'http.json'
        ]
        for path in possible_paths:
            if path.exists():
                try:
                    with open(path, 'r', encoding='utf-8') as f:
                        values = self._extract_values_from_text(f.read())
                    if 'port' in values: self.port = values['port']
                    if 'ip' in values:
                        raw = values['ip']
                        self.target_ip = '127.0.0.1' if raw == '0.0.0.0' else raw
                    logger.log('GAME', f'Конфиг SPT: {self.target_ip}:{self.port}', 'ok')
                    return
                except Exception:
                    pass
        
        logger.log('GAME', f'Использую стандартные настройки: {self.target_ip}:{self.port}', 'warn')

    def _is_port_open(self, host: str, port: int) -> bool:
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                s.settimeout(0.5)
                return s.connect_ex((host, port)) == 0
        except:
            return False

    def _terminate_process(self, process, name: str):
        if process is None: return

        if process.poll() is None: 
            # Используем print, т.к. логгер может затеряться в потоке сервера
            logger.log('FikaSync', f'Останавливаю {name}...')
            process.terminate()
            try:
                process.wait(timeout=3)
            except subprocess.TimeoutExpired:
                process.kill()

    def launch_and_monitor(self) -> bool:
        logger.log('GAME', 'Подготовка к запуску...')
        self._load_server_config()
        
        if not self.config.SPT_SERVER_PATH.exists():
            logger.log('GAME', 'Файл сервера не найден', 'error')
            return False
            
        try:
            # 1. ВИЗУАЛЬНОЕ РАЗДЕЛЕНИЕ
            print('\n')
            logger.log('FikaSync', '=' * 60)
            logger.log('FikaSync', f'Запуск сервера ({self.target_ip}:{self.port})')
            logger.log('FikaSync', '=' * 60)
            print('\n')

            # 2. ЗАПУСК СЕРВЕРА (В ЭТОМ ЖЕ ОКНЕ)
            # stdout=sys.stdout перенаправляет вывод сервера прямо в нашу консоль
            # stdin=subprocess.DEVNULL отключает ввод серверу, чтобы Enter достался Python-у
            self.server_process = subprocess.Popen(
                [str(self.config.SPT_SERVER_PATH)],
                cwd=self.config.SPT_SERVER_PATH.parent,
                stdout=sys.stdout,
                stderr=sys.stderr,
                stdin=subprocess.DEVNULL
            )
            
            # 3. ОЖИДАНИЕ ПОРТА
            # Мы не пишем логи здесь, чтобы не засорять вывод сервера
            server_started = False
            for _ in range(90):
                if self.server_process.poll() is not None:
                    print("\n[FikaSync] Сервер упал при запуске!")
                    return False
                
                if self._is_port_open(self.target_ip, self.port):
                    server_started = True
                    break
                time.sleep(1)
                
            if server_started:
                print(f"\n[FikaSync] Сервер доступен! Запускаю клиент...")
                self.launcher_process = subprocess.Popen(
                    [str(self.config.SPT_LAUNCHER_PATH)],
                    cwd=self.config.SPT_LAUNCHER_PATH.parent
                )
            else:
                print("\n[FikaSync] Таймаут ожидания порта. Пробуем запустить клиент...")
                self.launcher_process = subprocess.Popen(
                    [str(self.config.SPT_LAUNCHER_PATH)],
                    cwd=self.config.SPT_LAUNCHER_PATH.parent
                )

            # 4. ОЖИДАНИЕ ENTER
            print('\n')
            logger.log('FikaSync', '=' * 60)
            logger.log('FikaSync', 'Игра запущена')
            logger.log('FikaSync', 'Нажмите ENTER для завершения и синхронизации')
            logger.log('FikaSync', '=' * 60)
            print('\n')
            
            # Блокируем скрипт, пока пользователь не нажмет Enter
            # Даже если сервер спамит логами, Enter сработает
            input()
            
            logger.log('FikaSync', 'Завершение работы...', 'ok')
            self._terminate_process(self.launcher_process, 'Launcher')
            self._terminate_process(self.server_process, 'Server')
            
            return True

        except KeyboardInterrupt:
            logger.log('FikaSync', 'Принудительная остановка...', 'error')
            self._terminate_process(self.launcher_process, 'Launcher')
            self._terminate_process(self.server_process, 'Server')
            return True
            
        except Exception as e:
            logger.log('GAME', f'Ошибка: {e}', 'error')
            self._terminate_process(self.launcher_process, 'Launcher')
            self._terminate_process(self.server_process, 'Server')
            return False