using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogExtractorCore;

namespace LogExtractorApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectExcel_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtExcelPath.Text = ofd.FileName;
                }
            }
        }

        private void btnSelectLogFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "로그 파일들이 들어있는 폴더를 선택하세요.";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtLogFolderPath.Text = fbd.SelectedPath;
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            string excelPath = txtExcelPath.Text;
            string logFolderPath = txtLogFolderPath.Text;

            if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
            {
                MessageBox.Show("엑셀 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(logFolderPath) || !Directory.Exists(logFolderPath))
            {
                MessageBox.Show("로그 폴더를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            lblStatus.Text = "작업 시작...";
            txtStatusLog.Clear();
            progressBar1.Value = 0;

            try
            {
                // Security: Read Excel via ReadAllBytes
                txtStatusLog.AppendText("엑셀 파일 로드 중..." + Environment.NewLine);
                byte[] excelData = await Task.Run(() => File.ReadAllBytes(excelPath));

                string excelName = Path.GetFileNameWithoutExtension(excelPath);
                string outputFolder = Path.Combine(Path.GetDirectoryName(excelPath)!, excelName);

                var parser = new ExcelParser();
                var testCases = await Task.Run(() => parser.Parse(excelData));

                if (testCases.Count == 0)
                {
                    txtStatusLog.AppendText("분석된 테스트 케이스가 없습니다." + Environment.NewLine);
                    MessageBox.Show("분석된 테스트 케이스가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnStart.Enabled = true;
                    return;
                }

                txtStatusLog.AppendText($"총 {testCases.Count}개 케이스 분석 완료. 로그 추출을 시작합니다..." + Environment.NewLine);

                var logger = new Progress<string>(msg =>
                {
                    txtStatusLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                });

                var progress = new Progress<int>(v =>
                {
                    progressBar1.Value = v;
                });

                var processor = new LogProcessor();
                await Task.Run(() => processor.Process(logFolderPath, testCases, outputFolder, logger, progress));

                lblStatus.Text = "작업 완료!";
                txtStatusLog.AppendText("모든 작업이 완료되었습니다." + Environment.NewLine);
                MessageBox.Show($"작업이 완료되었습니다.\n저장 위치: {outputFolder}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                txtStatusLog.AppendText($"[오류] {ex.Message}{Environment.NewLine}");
                MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "오류 발생";
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }
    }
}
