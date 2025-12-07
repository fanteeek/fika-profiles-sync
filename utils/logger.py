import sys
import logging
from pathlib import Path
from datetime import datetime

class Logger:
    _instance = None  
    
    def __new__(cls):
        # Singleton
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if not self._initialized:
            self._initialized = True
            self.debug_enabled = False
            
            self._status_symbols = {
                '': '',
                'ok': '√',
                'error': '×',
                'warn': 'Δ'
            }
            
            self._setup_file_logging()
    
    def _setup_file_logging(self):
        try:
            if getattr(sys, 'frozen', False):
                base_dir = Path(sys.executable).parent
            else:
                base_dir = Path(__file__).parent.parent.absolute()
                
            log_dir = base_dir / 'logs'
            log_dir.mkdir(exist_ok=True)
            
            log_file = log_dir / 'fika_sync.log'
            
            self.file_logger = logging.getLogger('FikaSync')
            self.file_logger.setLevel(logging.DEBUG)
            
            if not self.file_logger.handlers:
                handler = logging.FileHandler(log_file, mode='w', encoding='utf-8')
                
                formatter = logging.Formatter(
                    '%(asctime)s | %(levelname)-8s | %(message)s',
                    datefmt='%Y-%m-%d %H:%M:%S'
                )
                handler.setFormatter(formatter)
                self.file_logger.addHandler(handler)
                
        except Exception as e:
            print(f'Ошибка настройки логгера: {e}')
            self.file_logger = logging.getLogger('Dummy')
            self.file_logger.addHandler(logging.NullHandler())

    def enable_debug(self):
        self.debug_enabled = True
        
    def disable_debug(self):
        self.debug_enabled = False
    
    def log(self, prefix: str, message: str, status: str = '') -> None:
        if prefix == 'DEBUG' and not self.debug_enabled:
            return
        
        timestamp = datetime.now().strftime('%H:%M:%S')
        prefix_padded = f'[{prefix}]'.ljust(10)
        status_symbol = self._status_symbols.get(status, '')
        
        console_msg = f'{timestamp} {prefix_padded} {status_symbol} {message}'
        print(' '.join(console_msg.split()))
        
        file_msg = f'[{prefix}] {message}'
        
        if self.file_logger:
            if status == 'error':
                self.file_logger.error(file_msg)
            elif status == 'warn':
                self.file_logger.warning(file_msg)
            else:
                self.file_logger.info(file_msg)
        