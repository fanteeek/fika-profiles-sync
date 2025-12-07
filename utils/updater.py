import requests
from datetime import datetime
from packaging import version
from utils.logger import Logger

logger = Logger()

class AutoUpdater:
    def __init__(self, config):
        self.config = config
        self.current_version = config.APP_VERSION
        self.repo_name = config.UPDATE_REPO_NAME

    def check_for_updates(self):
        logger.log('UPDATE', f'Проверка обновлений... (v{self.current_version})')
        
        try:
            url = f"https://api.github.com/repos/{self.repo_name}/releases/latest"
            
            # Таймаут поменьше, чтобы не задерживать запуск, если нет инета
            response = requests.get(url, timeout=3)
            
            if response.status_code != 200:
                logger.log('UPDATE', f'Не удалось проверить обновления (Code {response.status_code})', 'warn')
                return
                
            data = response.json()
            latest_tag = data.get('tag_name', '0.0.0').lstrip('v')
            published_at_str = data.get('published_at', '')
            html_url = data.get('html_url', '')
            
            # Сравнение версий
            v_current = version.parse(self.current_version)
            v_latest = version.parse(latest_tag)
            
            if v_latest > v_current:
                try:
                    dt = datetime.strptime(published_at_str, "%Y-%m-%dT%H:%M:%SZ")
                    formatted_date = dt.strftime("%d.%m.%Y %H:%M:%S")
                except ValueError:
                    formatted_date = published_at_str

                print('\n')
                logger.log('UPDATE', '=' * 60)
                logger.log('UPDATE', f'A new version is available! v{latest_tag}')
                logger.log('UPDATE', f'Released {formatted_date}')
                logger.log('UPDATE', f'Release Notes: {html_url}')
                logger.log('UPDATE', '=' * 60)
                print('\n')
            else:
                logger.log('UPDATE', 'У вас установлена актуальная версия', 'ok')

        except requests.RequestException:
            logger.log('UPDATE', 'Нет связи с GitHub для проверки обновлений', 'warn')
        except Exception as e:
            logger.log('UPDATE', f'Ошибка проверки обновлений: {e}', 'error')