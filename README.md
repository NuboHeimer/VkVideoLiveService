Модуль для [streamer.bot](https://streamer.bot/), позволяющий взаимодействовать со стриминговой площадкой [Vk Video Live](https://live.vkvideo.ru/). Для его работы необходимо задать имя канала в аргументе *channel_name* в экшене **Set channelName**.

Для общего использования готовы модули:

* Get New Viewers -- позволяет выводить в MiniChat новых зрителей, пришедших на трасляцию. Подробнее можно узнать в [этом гайде](https://dzen.ru/a/Zaq2Po_5TGRrHwx_).
  * ~~⚠ неактуально с 18.02.2025, так как теперь нет списка зрителей. Есть список *активных* зрителей. В который зритель попадает только после того, как напишет сообщение в чат.~~
  * ВК опять поменяли логику работы списка. Вероятно, теперь эта штука снова актуальна.
* Get Random Viewer -- позволяет получить случайного зрителя из списка *активных* зрителей. Подробнее можно узнать в [этом гайде](https://dzen.ru/a/ZWhq_W5vi2KFMEWF).
* Get Viewers -- позволяет получить список *активных* зрителей. Записывает список в аргумент users, аналогично тому, как работает **PresentViewers** в стримерботе.
* Get Viewers Count -- позволяет получить количество зрителей. Записывает его в аргумент стримербота *viewers_count*.
* Clear Todays Viewers -- позволяет очистить список "сегодняшних" зрителей. Нужно для корректного отображения новых зрителей. В качестве триггера можно использовать, например, триггер старта стрима.
  * ~~⚠ неактуально с 18.02.2025, так как теперь нет списка зрителей. Есть список *активных* зрителей. В который зритель попадает только после того, как напишет сообщение в чат.~~
  
В коде присутствуют методы, не готовые к широкому использованию, но если вы знаете, что делаете, то можете к ним обращаться:
* OnReward -- позволяет включить награду за баллы. Для его работы необоходимо задать аргументы:
  * *channelName* -- имя канала.
  * *rewardId* -- ID награды. Можно узнать через F12.
  * *token* -- ваш токен авторизации в Vk Video Live. Можно узнать через F12.
 
* OffReward -- позволяет выключить награду за баллы. Для его работы необоходимо задать аргументы:
  * *channelName* -- имя канала.
  * *rewardId* -- ID награды. Можно узнать через F12.
  * *token* -- ваш токен авторизации в Vk Video Live. Можно узнать через F12.
* GetTotalAverageViewrs -- получить среднее количество зрителей за всё время. (Или за последние 30 дней, я не помню :D) Для его работы необоходимо задать аргументы:
  * *channelName* -- имя канала.
  * *token* -- ваш токен авторизации в Vk Video Live. Можно узнать через F12.

По всем вопросам можете обращаться в [группу](https://t.me/nuboheimersb).

## License
This project is licensed under the [Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License](https://creativecommons.org/licenses/by-nc-sa/4.0/).  
**You are free to:**
- Share — copy and redistribute the code.
- Adapt — modify, transform, and build upon the code.

**Under the following terms:**
- Attribution — You must give appropriate credit.
- NonCommercial — You may not use the material for commercial purposes.
- ShareAlike — If you remix or modify the code, you must distribute your contributions under the same license.

See the [LICENSE](LICENSE) file for the full legal text.
