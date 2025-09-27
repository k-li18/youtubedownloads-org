const { chromium } = require('playwright');

async function testYouTubeDownloader() {
    console.log('Starting Playwright test for YouTube Downloader...');
    
    // Launch browser
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    
    try {
        // Navigate to our YouTube downloader
        console.log('Navigating to YouTube Downloader...');
        await page.goto('file:///Users/starkkevin/Downloads/youtube-downloader-web/frontend/index.html');
        
        // Wait for the page to load
        await page.waitForSelector('#title', { timeout: 5000 });
        
        // Get the title
        const title = await page.textContent('#title');
        console.log('Page title:', title);
        
        // Test URL input
        console.log('Testing URL input...');
        await page.fill('#videoUrl', 'https://www.youtube.com/watch?v=dQw4w9WgXcQ');
        
        // Get the input value to verify
        const inputValue = await page.inputValue('#videoUrl');
        console.log('Input value:', inputValue);
        
        // Check if download button exists and is enabled
        const downloadButton = await page.locator('#downloadBtn');
        const isEnabled = await downloadButton.isEnabled();
        console.log('Download button enabled:', isEnabled);
        
        // Take a screenshot
        await page.screenshot({ path: 'youtube-downloader-test.png' });
        console.log('Screenshot saved as youtube-downloader-test.png');
        
        console.log('✅ Playwright test completed successfully!');
        
    } catch (error) {
        console.error('❌ Test failed:', error);
    } finally {
        await browser.close();
    }
}

testYouTubeDownloader();