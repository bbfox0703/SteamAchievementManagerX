### **Key Modifications in This Fork**

1. **Change .Net version to 8.0**: .NET 8 brings long‑term support, performance improvements, modern C# features, single‑file publishing, trimming, and Native AOT options. Even though the Steamworks interaction keeps the application Windows‑only, these runtime enhancements can still reduce startup time and memory usage and provide a more maintainable foundation.
2. **Switch to x64 architecture**: Now it's a Windows 64-bit program.
3. **Search Function Improvement**: Allows users to search for achievements or descriptions, enhancing convenience and accuracy.  
4. **Multi-language Support**: SAM.Game will initially fetch achievements in the default language. Users can then select a preferred language and press "Refresh" to display achievements in the chosen language.  
5. **Countdown to Commit Achievements**: Users can set a countdown timer for committing achievements. Multiple achievements and their respective countdown durations can be configured, and the "Enable Timer" button will execute the countdown.
6. **Some UI tweaks**: Sorting by listview columns. Prevent Steam client show activicaty as idle (only when SAM.Game in foreground and turned-on).  
7. **Icon cache**: Implemented icon cache for SAM.Picker and SAM.Game. Cache folder is under program folder named "appcache".  
8. **Game list cache**: Implemented game list cache for SAM.Picker. Cache folder is under binaries folder named "appcache".  
9. **Error handling**: Many error handlings and reduce possible vulnerabilities.  

---

### 📝 **Disclaimer**

1. **Source**  
   This is a fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager). Many thanks to the original contributors for their excellent work on the foundational code.  

2. **Relationship with the Original Project**  
   This is an **independently maintained fork** and is **not officially affiliated** with the original project.  
   I do not intend to submit any changes or pull requests back to the original repository. This fork may include features and modifications that are not present in the original project.

3. **Purpose**  
   The primary goal of this fork is to add features and functionalities that I personally find useful or interesting (playground).  
   These changes are tailored to my own preferences and use cases.

4. **License and Copyright**  
   This project follows the same license as the original repository (e.g., MIT, GPL, etc.). For further details, please refer to the [LICENSE](LICENSE) file.  

5. **Disclaimer**  
   This software is provided "as-is" for educational and personal use only.  
   It is the user's responsibility to comply with any applicable laws and terms of service.

## Attribution

Most (if not all) icons are from:
* the [Fugue Icons](https://p.yusukekamiyamane.com/) set.  
* [Flaticon](https://www.flaticon.com/) free license
